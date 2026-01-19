# Todo List Application

A full-stack todo app built with Angular 21 and .NET 10, following Clean Architecture principles.

## What I Built

A secure, full-featured todo application with user authentication where you can:
- Register and login with JWT authentication
- Add new todos (private to your account)
- Mark them as complete/incomplete
- Edit todo titles inline
- Delete todos you don't need anymore

The backend is built with Clean Architecture (4 layers: Domain, Application, Infrastructure, API) and the frontend uses Angular's latest patterns - standalone components and signals. Security is implemented with ASP.NET Core Identity and JWT Bearer tokens.

## Tech Stack

**Backend**:
- .NET 10 Web API
- Clean Architecture (separation of concerns)
- ASP.NET Core Identity with JWT Bearer authentication
- Entity Framework Core with in-memory database
- AutoMapper for object mapping
- Global exception handling with ProblemDetails
- Health checks and structured logging
- 30 comprehensive tests (xUnit + FluentAssertions)

**Frontend**:
- Angular 21 with standalone components
- TypeScript + Signals for state management
- RxJS for handling async operations
- Vitest for testing

## Running the Application

You'll need .NET 10 SDK and Node.js installed.

**Start the backend**:
```bash
cd backend/src/TodoList.Api
dotnet run
```
API runs at `http://localhost:5000` - you can check the Swagger docs at `http://localhost:5000/swagger`.

**Start the frontend** (in a new terminal):
```bash
cd frontend
npm install
npm start
```
App runs at `http://localhost:4200`

### Using the Application

1. **Register a new account:**
   - Click "Register" on the login page
   - Enter an email and password
   - Password requirements: minimum 6 characters, at least 1 digit, 1 lowercase, 1 uppercase, and 1 non-alphanumeric character
   - Example: `test@example.com` / `Test123!`

2. **Login:**
   - Use your registered credentials to login
   - A JWT token will be stored in session storage
   - The token is automatically included in all API requests via HTTP interceptor

3. **Manage Todos:**
   - Add new todos using the input field
   - Click the checkbox to mark todos as complete/incomplete
   - Click the edit icon to modify a todo's title
   - Click the delete icon to remove a todo
   - All todos are private to your account

### API Endpoints

All endpoints are documented in Swagger UI at `http://localhost:5000/swagger`. Key endpoints:

- `POST /api/v1/auth/register` - Register a new user
- `POST /api/v1/auth/login` - Login and receive JWT token
- `GET /api/v1/todos` - Get all todos for authenticated user
- `POST /api/v1/todos` - Create a new todo
- `PUT /api/v1/todos/{id}` - Update a todo
- `DELETE /api/v1/todos/{id}` - Delete a todo
- `GET /health` - Health check endpoint

## Testing

**Backend tests** (30 tests covering all business logic):
```bash
cd backend
dotnet test
```

**Frontend tests** (service layer testing):
```bash
cd frontend
npm test
```

## Design Decisions

**Why Clean Architecture?**
I wanted to demonstrate that I can design systems that are maintainable and testable. The layers are completely decoupled - you could swap the in-memory storage for SQL Server without touching the business logic. Same goes for the API layer.

**Why such comprehensive backend testing?**
As a backend-focused developer, testing is where I spend a lot of my time in real projects. I included both unit tests (mocking dependencies) and integration tests (full HTTP request/response cycles) to show I understand different testing strategies.

**Why basic frontend testing?**
I'm honest about my experience level - I'm stronger on the backend. I included service layer tests to demonstrate I understand testing principles across the stack, but I focused my effort where I provide the most value.

## What Would Change for Production

- Replace in-memory database with a real database (SQL Server or PostgreSQL with migrations)
- Implement pagination for the todo list
- Add rate limiting to prevent abuse
- Add more restrictive CORS policies (specific origins, not wildcard)
- Use distributed caching (Redis) for session management
- Add comprehensive monitoring and alerting (Application Insights, Serilog)
- Implement refresh tokens for better security
- Add account lockout after failed login attempts
- Deploy with CI/CD pipeline (GitHub Actions, Azure DevOps)
- Use environment-specific configuration (Azure Key Vault for secrets)
- Add API versioning strategy for backward compatibility
- Implement soft deletes for todos

## Project Structure

```
backend/
├── src/
│   ├── TodoList.Domain/          # Entities and interfaces
│   ├── TodoList.Application/     # Business logic and DTOs
│   ├── TodoList.Infrastructure/  # Data access
│   └── TodoList.Api/             # REST API controllers
└── tests/                        # 30 comprehensive tests

frontend/src/app/
├── models/                       # TypeScript interfaces
├── services/                     # API communication
└── components/                   # Angular components
```

## A Few Notes

- The backend follows RESTful conventions (proper HTTP verbs, status codes, etc.)
- I used async/await throughout even though it's not strictly necessary for in-memory storage - designed for the future when we add a real database
- CORS is configured to allow the Angular frontend to communicate with the API
- All API endpoints are documented with Swagger/OpenAPI with JWT Bearer authentication support
- JWT tokens are configured with proper validation (issuer, audience, signing key, expiration)
- All error responses follow the ProblemDetails standard (RFC 7807)
- API versioning is implemented with `/api/v1/` prefix for future compatibility
- Structured logging is implemented throughout the application
- Password validation follows security best practices (complexity requirements)
- User isolation ensures todos are private to each account

---

Thanks for reviewing my project. I treated this as a real project and tried to demonstrate production-quality code and architecture, not just "make it work." Happy to discuss any decisions I made or walk through the code in an interview.
