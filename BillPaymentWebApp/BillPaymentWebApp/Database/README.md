# Partner database

Run `001_partner_schema.sql` and `002_partner_seed.sql` to create and seed the `BillPaymentPartners` database. The application runs `003_jambank_schema.sql` automatically at startup to create the separate JamBank simulation tables and demo accounts if any of them are missing. The JamBank tables have `JamBank*` names and the migration does not alter or reset partner tables.

The shared tables use `Provider` (`FLOW` or `SAGICOR`) as a discriminator. `PartnerTransactions` has a foreign key to `PartnerAccounts`, a unique `(Provider, TransactionReference)` constraint, and indexes for account history and lookup performance.
