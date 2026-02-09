# Security Implementation Guide

## Overview
This project implements enterprise-grade security features including:
- JWT-based authentication
- Refresh token rotation
- Session management
- Rate limiting
- Security audit logging
- CORS protection

## Route Changes
The following authentication routes have been updated for better consistency:
- Registration: `/account/new` -> `/account/register`
- Login: `/account/enter` -> `/account/login`
- Refresh Token: (New) `/account/refresh`
- Logout: (New) `/account/logout`
- Logout All Devices: (New) `/account/logout-all`

## Setup Instructions

### 1. Configure Secrets

#### Development Environment
```bash
cd src/Equinox.Services.Api
dotnet user-secrets init
dotnet user-secrets set "AppSettings:SecretKey" "YOUR_GENERATED_SECRET_KEY"
```

Generate a secure key using the `SecretKeyGenerator` utility or any secure random base64 string (32+ bytes).

#### Production Environment
Set environment variables:
```bash
export EQUINOX_AppSettings__SecretKey="YOUR_PRODUCTION_SECRET_KEY"
export EQUINOX_ConnectionStrings__DefaultConnection="YOUR_DB_CONNECTION"
```

### 2. Database Migrations
The security features require new tables in the Identity database.

```bash
cd src/Equinox.Infra.CrossCutting.Identity
dotnet ef database update --context EquinoxIdentityContext
```

### 3. Configure CORS
Update `appsettings.json` or environment variables:
```json
{
  "AllowedOrigins": [
    "https://yourdomain.com",
    "https://app.yourdomain.com"
  ]
}
```

## API Usage

### Authentication Flow

#### 1. Register
```http
POST /account/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

#### 2. Login
```http
POST /account/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

#### 3. Refresh Token
```http
POST /account/refresh
Content-Type: application/json

{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123..."
}
```

#### 4. Logout
```http
POST /account/logout
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "refreshToken": "abc123..."
}
```

## Security Features

### 1. Rate Limiting
- **Authentication endpoints**: 5 requests/minute per IP
- **API endpoints**: 60 requests/minute per user

### 2. Token Security
- Access tokens: 1 hour expiration
- Refresh tokens: 7 days expiration
- Automatic token rotation on refresh
- One-time use refresh tokens

### 3. Session Tracking
- Device fingerprinting based on User-Agent and IP
- Session activity monitoring and revocation support

### 4. Audit Logging
All security events are logged to the `SecurityAuditLogs` table and standard ILogger.
