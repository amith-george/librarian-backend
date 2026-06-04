# Library Management System API

An optimized .NET 8 Web API for managing book tracking, paginated browsing, and high-performance NgRx autocomplete lookups.

> ⚠️ **CRITICAL:** Check the **`release`** branch for the Long-Term Support (LTS) version.

## Steps to Run Using Docker

1. **Build the Docker Image:**
   ```bash
   docker build -t library-api .
2.Run the Container:

  ```Bash
  docker run -d -p 8080:8080 --name library-api-instance library-api
