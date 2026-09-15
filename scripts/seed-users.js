/**
 * Provisions users directly in MongoDB. There is no registration endpoint
 * anywhere in the system by design - granting or revoking access is
 * purely a database change, made by whoever runs this script.
 *
 * Setup (one-time):
 *   npm install --no-save mongodb bcryptjs
 *
 * Usage:
 *   MONGO_CONNECTION_STRING="mongodb+srv://..." node scripts/seed-users.js
 *
 * Edit the USERS array below first. Never commit real passwords - this
 * file is meant to be edited locally and the plaintext values discarded
 * immediately after running.
 */
const { MongoClient } = require("mongodb");
const bcrypt = require("bcryptjs");

const USERS = [
  { username: "admin1", plaintextPassword: "@Admin1234", role: "Admin" },
  { username: "trader1", plaintextPassword: "@Trader1234", role: "Trader" },
];

const DATABASE_NAME = "renewable_assets";

async function main() {
  const connectionString = process.env.MONGO_CONNECTION_STRING;
  if (!connectionString) {
    console.error("Set MONGO_CONNECTION_STRING before running this script.");
    process.exit(1);
  }

  const client = new MongoClient(connectionString);

  try {
    await client.connect();
    const users = client.db(DATABASE_NAME).collection("users");
    

    for (const u of USERS) {
      const existing = await users.findOne({ username: u.username });
      if (existing) {
        console.log(`Skipping ${u.username} - already exists.`);
        continue;
      }

      const passwordHash = bcrypt.hashSync(u.plaintextPassword, 10);
      await users.insertOne({ username: u.username, passwordHash, role: u.role });
      console.log(`Created user "${u.username}" with role "${u.role}".`);
    }
  } finally {
    await client.close();
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
