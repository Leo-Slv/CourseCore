// Local demo data for manual validation. Talks to a running API (Development) and
// uses a throwaway postgres container only to confirm the test users' e-mails.
//
//   node scripts/seed-demo-data.mjs
//
// Env (all optional): API_URL, ADMIN_EMAIL, ADMIN_PASSWORD, DB_NAME, DB_HOST.
import { execFileSync } from 'node:child_process';

const API = process.env.API_URL ?? 'http://localhost:5080';
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin.e2e@coursecore.local';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'E2eAdmin!Passw0rd-123';
const DB_NAME = process.env.DB_NAME ?? 'coursecore_demo';
const DB_HOST = process.env.DB_HOST ?? 'host.docker.internal';
const DEMO_PASSWORD = 'Demo!Senha-2026';

async function call(method, path, token, body) {
	const response = await fetch(`${API}${path}`, {
		method,
		headers: {
			'Content-Type': 'application/json',
			...(token ? { Authorization: `Bearer ${token}` } : {}),
		},
		body: body === undefined ? undefined : JSON.stringify(body),
	});
	const text = await response.text();
	const data = text ? JSON.parse(text) : null;
	if (!response.ok) {
		throw new Error(`${method} ${path} -> ${response.status} ${text}`);
	}
	return data;
}

const login = async (email, password) =>
	(await call('POST', '/api/auth/login', null, { email, password })).token.accessToken;

function sql(statement) {
	execFileSync('docker', [
		'run', '--rm', '-e', 'PGPASSWORD=root', 'postgres:17-alpine',
		'psql', '-h', DB_HOST, '-U', 'postgres', '-d', DB_NAME, '-c', statement,
	], { stdio: 'pipe' });
}

const admin = await login(ADMIN_EMAIL, ADMIN_PASSWORD);

// --- areas -----------------------------------------------------------------
const areaBody = (name, slug, order, accentColor, isPublic) => ({
	name, slug, description: name, displayOrder: order, accentColor, imageUrl: null, isPublic,
});
const lideranca = await call('POST', '/api/areas', admin, areaBody('Liderança', 'lideranca', 10, 'Blue', true));
const equipe = await call('POST', '/api/areas', admin, areaBody('Equipe', 'equipe', 20, 'Green', false));

// --- courses ---------------------------------------------------------------
const lessons = (prefix) => [
	{ title: `${prefix} — Aula 1`, description: 'Introdução', displayOrder: 1, freePreview: true },
	{ title: `${prefix} — Aula 2`, description: 'Aprofundamento', displayOrder: 2, freePreview: false },
];
const modules = (prefix) => [
	{ title: 'Módulo 1', description: 'Fundamentos', displayOrder: 1, lessons: lessons(`${prefix} M1`) },
	{ title: 'Módulo 2', description: 'Prática', displayOrder: 2, lessons: lessons(`${prefix} M2`) },
];
const courseDefs = [
	{ title: 'Fundamentos da Fé', slug: 'fundamentos-da-fe', pricingModel: 'Free', areas: [lideranca], featured: true },
	{ title: 'Escola de Líderes', slug: 'escola-de-lideres', pricingModel: 'Paid', priceAmount: 50, areas: [lideranca], featured: true },
	{ title: 'Discipulado Avançado', slug: 'discipulado-avancado', pricingModel: 'Paid', priceAmount: 120, areas: [lideranca] },
	{ title: 'Retiro de Líderes', slug: 'retiro-de-lideres', pricingModel: 'EnrollmentControlled', areas: [lideranca] },
	{ title: 'Formação Interna', slug: 'formacao-interna', pricingModel: 'Free', areas: [equipe] },
];
const courses = {};
for (const [index, def] of courseDefs.entries()) {
	const course = await call('POST', '/api/courses', admin, {
		title: def.title,
		slug: def.slug,
		description: `${def.title} — curso de demonstração.`,
		thumbnailUrl: null,
		displayOrder: index + 1,
		pricingModel: def.pricingModel,
		priceAmount: def.priceAmount ?? null,
		issuesCertificate: true,
		isFeatured: Boolean(def.featured),
		areaIds: def.areas.map((area) => area.id),
		modules: modules(def.title),
	});
	await call('POST', `/api/courses/${course.id}/publish`, admin);
	courses[def.slug] = course;
}

// --- roles (backoffice) ----------------------------------------------------
const secretaria = await call('POST', '/api/admin/roles', admin, { name: 'Secretaria', description: 'Equipe da secretaria' });
await call('PUT', `/api/admin/roles/${secretaria.id}/permissions`, admin, { permissionKeys: ['visitors.read', 'visitors.manage'] });
const midia = await call('POST', '/api/admin/roles', admin, { name: 'Mídia', description: 'Equipe de mídia e conteúdo' });
await call('PUT', `/api/admin/roles/${midia.id}/permissions`, admin, { permissionKeys: ['courses.manage', 'videos.manage'] });

// --- users -----------------------------------------------------------------
const users = [
	{ name: 'Ana Aluna', email: 'aluna@demo.local', role: null },
	{ name: 'Bruno Aluno', email: 'aluno@demo.local', role: null },
	{ name: 'Maria Secretaria', email: 'secretaria@demo.local', role: secretaria },
	{ name: 'Marcos Mídia', email: 'midia@demo.local', role: midia },
	{ name: 'Paulo Admin', email: 'admin2@demo.local', role: { id: (await call('GET', '/api/roles', admin)).find((r) => r.name === 'Admin').id } },
];
const created = {};
for (const user of users) {
	created[user.email] = await call('POST', '/api/users', admin, { name: user.name, email: user.email, password: DEMO_PASSWORD });
	if (user.role) await call('POST', `/api/users/${created[user.email].id}/roles/${user.role.id}`, admin);
}
// Accounts created by an admin start unconfirmed; confirm them so every user can sign in normally.
sql(`update users set "EmailVerifiedAt" = now() where "Email" like '%@demo.local'`);

// Bruno bought "Escola de Líderes" in the past: give him per-course access.
await call('POST', '/api/access/requests/grant', admin, { userId: created['aluno@demo.local'].id, courseId: courses['escola-de-lideres'].id });

// --- visitors ("Primeira vez") ----------------------------------------------
const visitors = [
	['Carla Dias', '(31) 3456-7890', 'carla@example.com', 'Av. Brasil, 55'],
	['João Pereira', '(21) 97777-2222', 'joao@example.com', null],
	['Maria Souza', '(11) 98888-1111', 'maria@example.com', 'Rua das Flores, 100'],
];
for (const [name, phone, email, address] of visitors) {
	await call('POST', '/api/visitors', null, { name, phone, email, address, captchaToken: 'dev-token' }).catch((e) => console.warn('visitor skipped:', e.message));
}

console.log('\nDemo data ready.\n');
console.log('Senha de todos os usuários demo:', DEMO_PASSWORD);
console.log('Admin (seed):', ADMIN_EMAIL, '/', ADMIN_PASSWORD);
