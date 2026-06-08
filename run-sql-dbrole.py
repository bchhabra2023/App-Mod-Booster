#!/usr/bin/env python3
"""Execute SQL role script using Azure AD authentication."""
import os
import subprocess

SERVER = os.getenv("SQL_SERVER_FQDN", "example.database.windows.net")
DATABASE = os.getenv("SQL_DATABASE_NAME", "northwind")
SQL_SCRIPT_FILE = "script.sql"

if __name__ == "__main__":
    subprocess.run([
        "az", "sql", "db", "query",
        "--server", SERVER.split(".")[0],
        "--database", DATABASE,
        "--auth-mode", "ActiveDirectoryDefault",
        "--file", SQL_SCRIPT_FILE
    ], check=True)
