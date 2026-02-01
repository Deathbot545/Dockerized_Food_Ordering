# Dockerized Food Ordering (Local)

## Run locally with Docker Desktop (Windows)

1. (Optional) Copy the sample environment file if you want to customize defaults:
   ```bash
   cp .env.example .env
   ```
2. Build and start everything:
   ```bash
   docker compose up -d --build
   ```

### Local URLs

- Food Ordering Web: http://localhost:8088
- Kitchen Web: http://localhost:8089
- Food Ordering API: http://localhost:5100
- Restaurant API: http://localhost:5101
- Order API: http://localhost:5104
- Menu API: http://localhost:5105

### Local data services

- Postgres: localhost:5432 (databases ApplicationDb, OutletDb, MenuDb are created on first run)
- MongoDB: localhost:27017

### Stop services

```bash
docker compose down
```
