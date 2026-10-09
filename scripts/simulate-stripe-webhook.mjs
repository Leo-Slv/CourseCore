// Local-testing helper: plays Stripe's part after a checkout, by posting a
// correctly signed `checkout.session.completed` webhook for an order.
//
// Needed when you run against stripe-mock (or without a Stripe account), where
// there is no real hosted payment page that would call the webhook for you.
//
//   node scripts/simulate-stripe-webhook.mjs [orderId] [--status=paid|unpaid] [--type=checkout.session.completed]
//
// Without an orderId it picks the most recent Pending order. Order data is read
// straight from PostgreSQL through a throwaway `postgres` docker container, so
// no extra npm packages are needed.
//
// Environment (same names the API uses):
//   ConnectionStrings__CourseCoreDatabase   Npgsql connection string of the API's database
//   Stripe__WebhookSecret                   the API's webhook signing secret
//   API_URL                                 defaults to http://localhost:5080
import { execFileSync } from 'node:child_process';
import crypto from 'node:crypto';

const args = process.argv.slice(2);
const flag = (name, fallback) =>
	args.find((arg) => arg.startsWith(`--${name}=`))?.split('=')[1] ?? fallback;
const orderIdArg = args.find((arg) => !arg.startsWith('--'));

const connectionString = process.env.ConnectionStrings__CourseCoreDatabase;
const secret = process.env.Stripe__WebhookSecret;
const apiUrl = process.env.API_URL ?? 'http://localhost:5080';

if (!connectionString || !secret) {
	console.error(
		'Set ConnectionStrings__CourseCoreDatabase and Stripe__WebhookSecret (same values the API runs with).',
	);
	process.exit(1);
}

const part = (key) =>
	connectionString
		.split(';')
		.map((item) => item.split('='))
		.find(([name]) => name.trim().toLowerCase() === key)?.[1];

const where = orderIdArg
	? `"Id" = '${orderIdArg.replace(/[^0-9a-fA-F-]/g, '')}'`
	: `"Status" = 0 ORDER BY "CreatedAt" DESC LIMIT 1`;
const host = part('host');
const row = execFileSync(
	'docker',
	[
		'run',
		'--rm',
		'-e',
		`PGPASSWORD=${part('password')}`,
		'postgres:16-alpine',
		'psql',
		'-h',
		host === '127.0.0.1' || host === 'localhost' ? 'host.docker.internal' : host,
		'-p',
		part('port') ?? '5432',
		'-U',
		part('username'),
		'-d',
		part('database'),
		'-At',
		'-F',
		'|',
		'-c',
		`SELECT "Id", "StripeCheckoutSessionId", "AmountInCents", "Currency" FROM orders WHERE ${where};`,
	],
	{ encoding: 'utf8' },
).trim();

if (!row) {
	console.error('No matching order found.');
	process.exit(1);
}

const [orderId, sessionId, amountInCents, currency] = row.split('|');
const type = flag('type', 'checkout.session.completed');
const paymentStatus = flag('status', 'paid');

const payload = JSON.stringify({
	id: `evt_${crypto.randomUUID().replaceAll('-', '')}`,
	object: 'event',
	api_version: '2025-01-27.acacia',
	created: Math.floor(Date.now() / 1000),
	type,
	livemode: false,
	data: {
		object: {
			id: sessionId,
			object: 'checkout.session',
			client_reference_id: orderId,
			payment_status: paymentStatus,
			amount_total: Number(amountInCents),
			currency,
		},
	},
});
const timestamp = Math.floor(Date.now() / 1000);
const signature = crypto
	.createHmac('sha256', secret)
	.update(`${timestamp}.${payload}`)
	.digest('hex');

const response = await fetch(`${apiUrl}/api/payments/stripe/webhook`, {
	method: 'POST',
	headers: {
		'Content-Type': 'application/json',
		'Stripe-Signature': `t=${timestamp},v1=${signature}`,
	},
	body: payload,
});

console.log(
	`${type} (${paymentStatus}) for order ${orderId} -> HTTP ${response.status}`,
);
