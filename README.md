# 🚀 Orders API - .NET 9

![.NET](https://img.shields.io/badge/.NET-9-blue)
![Docker](https://img.shields.io/badge/Docker-Ready-blue)
![Azure](https://img.shields.io/badge/Azure-Container%20Apps-blue)
![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-green)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)

API REST desarrollada en **ASP.NET Core (.NET 9)** con enfoque **Database First**, arquitectura por capas, seguridad JWT, pruebas unitarias y despliegue en Azure.

---

## 🌐 Demo

Swagger disponible en:

👉 https://orders-api-app.salmonrock-f04010b7.northeurope.azurecontainerapps.io/

---

## 🧠 Descripción

Este proyecto demuestra un flujo backend completo:

- Desarrollo de API RESTful
- Diseño de base de datos en SQL Server
- Uso de Entity Framework Core (Database First)
- Ejecución de procesos batch con Dapper
- Autenticación JWT
- Dockerización
- CI/CD con GitHub Actions
- Despliegue en Azure Container Apps
- Rate limiting y health checks

---

## 🏗️ Arquitectura

Controller → Service → Repository → Database

---

## 📂 Estructura

Controllers/     → Endpoints HTTP  
Services/        → Lógica de negocio  
Repositories/    → Acceso a datos (EF + Dapper)  
Interfaces/      → Contratos  
DTOs/            → Entrada / salida (Data Transfer Objects)  
Entities/        → Modelos generados desde BD  
Data/            → DbContext  
BackgroundJobs/  → Procesos async  
Middlewares/     → Pipeline HTTP  

---

## ⚙️ Funcionalidades

### 🛒 Crear pedido

POST /api/Orders  

✔ Valida datos  
✔ Calcula total  
✔ Maneja duplicados con índice único  

Respuestas:

- 201 Created  
- 409 Conflict  

---

### 📄 Listar pedidos (paginado)

GET /api/Orders?page=1&pageSize=10  

✔ Paginación eficiente (Skip / Take)  
✔ AsNoTracking para mejor rendimiento  

---

### 🔄 Reproceso batch

POST /api/Jobs/reprocess-orders  

✔ Ejecuta Stored Procedure  
✔ Usa Dapper  
✔ Registra estado del job  

GET /api/Jobs/{id}  

---

### ❤️ Health Check

GET /health  

---

## 🔐 Seguridad

- Autenticación JWT  
- Swagger con soporte Bearer Token  
- Rate limiting: 60 requests/min por IP  

---

## ⚡ Performance

- Paginación  
- AsNoTracking  
- Procesamiento en SQL Server (SP)  
- Uso de Dapper para operaciones batch  

---

## 🧱 Base de datos

Diseñada en SQL Server con:

- Relaciones (FK)  
- Índices  

Índice único para concurrencia:

CREATE UNIQUE INDEX UX_Orders_ExternalReference  
ON Orders(ExternalReference);  

---

## 🧪 Testing

Proyecto:

OrdersApi.Tests  

Incluye pruebas unitarias sobre lógica de negocio.

---

## 🐳 Docker

docker build -t orders-api-dotnet .  
docker run -p 8080:8080 orders-api-dotnet  

---

## 🔄 CI/CD

GitHub Actions automatiza:

Build → Test → Docker → Deploy  

---

## ☁️ Azure

Desplegado en:

- Azure Container Apps  
- Escalado controlado  
- Integración con Docker Hub  

---

## 🛡️ Buenas prácticas aplicadas

- Clean Architecture (por capas)  
- Principios SOLID  
- DTOs para desacoplamiento  
- Database First  
- Manejo de concurrencia en BD  
- Uso correcto de códigos HTTP  
- Separación de responsabilidades  
- Seguridad y control de acceso  
- Protección contra abuso (rate limiting)  

---

## 🧠 Qué demuestra este proyecto

✔ Desarrollo backend completo  
✔ Pensamiento orientado a arquitectura  
✔ Experiencia real con SQL Server  
✔ Integración cloud (Azure)  
✔ Uso de contenedores  
✔ Resolución de problemas en producción  

---

## 👨‍💻 Autor

Proyecto desarrollado como ejercicio práctico para demostrar habilidades en desarrollo backend con .NET, SQL Server, Docker y Azure.
