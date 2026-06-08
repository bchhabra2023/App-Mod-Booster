#!/usr/bin/env python3
"""Execute SQL script on Azure SQL Database using Azure Active Directory authentication."""
import os
import subprocess
import tempfile
from azure.identity import AzureCliCredential

SERVER = os.getenv("SQL_SERVER_FQDN", "example.database.windows.net")
DATABASE = os.getenv("SQL_DATABASE_NAME", "northwind")
SQL_SCRIPT_FILE = "Database-Schema/database_schema.sql"


def run():
    AzureCliCredential().get_token("https://management.azure.com/.default")
    with open(SQL_SCRIPT_FILE, "r", encoding="utf-8") as source:
        raw = source.read()

    batches = []
    current = []
    for line in raw.splitlines():
        if line.strip().upper() == "GO":
            if current:
                batches.append("\n".join(current))
                current = []
        else:
            current.append(line)
    if current:
        batches.append("\n".join(current))

    for idx, batch in enumerate(batches, start=1):
        if not batch.strip():
            continue
        with tempfile.NamedTemporaryFile("w", suffix=".sql", delete=False, encoding="utf-8") as tmp:
            tmp.write(batch)
            temp_path = tmp.name

        print(f"Executing schema batch {idx}/{len(batches)}")
        subprocess.run([
            "az", "sql", "db", "query",
            "--server", SERVER.split(".")[0],
            "--database", DATABASE,
            "--auth-mode", "ActiveDirectoryDefault",
            "--file", temp_path
        ], check=True)
        os.remove(temp_path)


if __name__ == "__main__":
    run()
