# Delivery CSV Importer Worker Service

This is a **.NET Worker Service** designed to automatically import CSV files containing delivery schedules into a PostgreSQL database. The service monitors a folder for CSV files, processes them, logs results, and moves files to appropriate directories.

---

## Features

- Automatically monitors a source folder for CSV files.
- Validates CSV format (expects 19 columns in the header).
- Inserts data into PostgreSQL using parameterized queries.
- Moves successfully processed files to a destination folder.
- Moves failed files to an error folder.
- Logs all processing activity to a log folder.
- Configurable via `appsettings.json` or environment variables.
- Runs as a background service (Worker Service).

---
