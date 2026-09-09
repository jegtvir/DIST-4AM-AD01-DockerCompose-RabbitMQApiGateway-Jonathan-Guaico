# Sistema Distribuido de Gestión de Vehículos y Categorías con RabbitMQ, ApiGateway y Autenticación JWT

* **Asignatura:** Aplicaciones Distribuidas (4to Nivel)
* **Actividad:** Trabajo Autónomo (AA) - Docker Compose, RabbitMQ, API Gateway y Despliegue en Azure
* **Autor:** Jonathan Guaico
* **Período:** Septiembre 2026
* **Repositorio GitHub:** [DIST-4AM-AD01-DockerCompose-RabbitMQApiGateway-Jonathan-Guaico](https://github.com/jegtvir/DIST-4AM-AD01-DockerCompose-RabbitMQApiGateway-Jonathan-Guaico)

---

## 📹 Video Demostrativo (Máximo 5 minutos)

* **Enlace del Video:** 
* **Contenido de la demostración:**
  1. Acceso a los servicios desplegados en la nube de Microsoft Azure (`158.23.165.154`).
  2. Autenticación y generación de Token JWT en el microservicio `Seguridad.Api` (roles `Administrador` y `Usuario`).
  3. Consumo de endpoints protegidos mediante el **API Gateway** y de forma directa.
  4. Demostración de la comunicación asíncrona mediante eventos a través del broker de mensajería **RabbitMQ**.
  5. Inspección del Dashboard de RabbitMQ con colas y mensajes en tiempo real.

---

## 🌐 Servicios Desplegados en Azure (Enlaces Ejecutables)

Los siguientes servicios se encuentran desplegados y 100% operativos en Microsoft Azure en la dirección IP pública **`158.23.165.154`**:

| Servicio | Tipo / Protocolo | Enlace Directo / URL Ejecutable | Credenciales de Prueba |
| :--- | :---: | :--- | :--- |
| **Seguridad API** | Swagger UI / REST | [http://158.23.165.154:5260/swagger](http://158.23.165.154:5260/swagger) | `admin` / `Admin123*` <br> `usuario` / `User123*` |
| **Categoría API** | Swagger UI / REST | [http://158.23.165.154:5259/swagger](http://158.23.165.154:5259/swagger) | Requiere Bearer JWT |
| **Vehículo API** | Swagger UI / REST | [http://158.23.165.154:5258/swagger](http://158.23.165.154:5258/swagger) | Requiere Bearer JWT |
| **API Gateway** | YARP Reverse Proxy | [http://158.23.165.154:5119](http://158.23.165.154:5119) | Enrutamiento centralizado |
| **RabbitMQ Dashboard** | Management UI | [http://158.23.165.154:15672](http://158.23.165.154:15672) | **User:** `admin`<br>**Pass:** `admin123` |

> ℹ️ **Disponibilidad:** Conforme a lo indicado en los requisitos, los recursos en Azure permanecerán activos de manera continua durante el período de evaluación académica (mínimo hasta el domingo 13/09/2026).

---

## 🏗️ Arquitectura del Sistema

La solución está diseñada bajo un patrón de **Microservicios Desacoplados**, con comunicación síncrona mediante **REST / HTTP** centralizada a través de un **API Gateway (YARP)**, seguridad basada en **Tokens JWT (OAuth2 Bearer)** y comunicación asíncrona reactiva guiada por eventos mediante **RabbitMQ**.

```mermaid
flowchart TD
    subgraph Clientes["Clientes Externos"]
        User["Docente / Cliente / Postman"]
    end

    subgraph GatewayLayer["Capa de Entrada y Enrutamiento"]
        GW["API Gateway (YARP)\nPuerto: 5119\nRutas: /api/auth/*, /api/categoria/*, /api/vehiculo/*"]
    end

    subgraph Microservicios["Microservicios Backend (.NET)"]
        SecApi["Seguridad.Api\nPuerto: 5260\nJWT / BCrypt"]
        CatApi["Categoria.Api\nPuerto: 5259\nCRUD Categorías + Publisher"]
        VehApi["Vehiculo.Api\nPuerto: 5258\nCRUD Vehículos + Consumer"]
    end

    subgraph EventBroker["Message Broker Asíncrono"]
        RMQ["RabbitMQ Broker\nAMQP: 5672 | UI: 15672\nExchange / Queues"]
    end

    subgraph DataLayer["Capa de Persistencia"]
        SQL["SQL Server Container\nPuerto: 1433\nBD: Seguridad, categoria, vehciulos"]
    end

    User -->|HTTP Requests| GW
    User -.->|Acceso Swagger Directo| SecApi
    User -.->|Acceso Swagger Directo| CatApi
    User -.->|Acceso Swagger Directo| VehApi

    GW -->|Proxy /api/auth| SecApi
    GW -->|Proxy /api/categoria| CatApi
    GW -->|Proxy /api/vehiculo| VehApi

    CatApi -->|Publica Eventos (Creación/Actualización)| RMQ
    RMQ -->|Consume Eventos de Categoría| VehApi

    SecApi -->|EF Core / ADO| SQL
    CatApi -->|EF Core| SQL
    VehApi -->|EF Core| SQL
```

---

## 📦 Descripción de los Microservicios y Componentes

### 1. ApiGateway (Reverse Proxy con YARP)
* Actúa como el punto único de entrada (*Single Point of Entry*) para el cliente.
* Enruta el tráfico hacia los microservicios correspondientes:
  * `/api/auth/{**catch-all}` ➡️ `Seguridad.Api:8080`
  * `/api/categoria/{**catch-all}` ➡️ `Categoria.Api:8080`
  * `/api/vehiculo/{**catch-all}` ➡️ `Vehiculo.Api:8080`
* Oculta la topología interna y la infraestructura privada de la red de contenedores.

### 2. Seguridad.Api (Servicio de Autenticación OAuth / JWT)
* Administra el registro y login de usuarios con contraseñas encriptadas mediante el algoritmo **BCrypt**.
* Genera tokens **JSON Web Token (JWT)** firmados criptográficamente (HMAC-SHA256).
* Emite los siguientes claims clave: `nameidentifier` (ID), `name` (Username), `email`, `role` (`Administrador` o `Usuario`), `jti` y tiempo de expiración.
* Provee la validación de credenciales para el control de acceso en los demás microservicios.

### 3. Categoria.Api (Gestión de Categorías)
* Microservicio encargado del ciclo de vida (CRUD) de las categorías vehiculares.
* Protección de endpoints mediante atributos `[Authorize]`:
  * Métodos de lectura (`GET`): Disponibles para roles `Administrador` y `Usuario`.
  * Métodos de escritura (`POST`, `PUT`, `DELETE`): Exclusivos para el rol `Administrador`.
* **Integración con RabbitMQ:** Al crear o actualizar una categoría, publica un evento asíncrono en el exchange/cola de RabbitMQ para notificar a los suscriptores.

### 4. Vehiculo.Api (Gestión de Vehículos)
* Administra el catálogo de vehículos (marca, modelo, precio, stock, estado) vinculados a una categoría.
* Protegido mediante validación de token JWT.
* **Integración con RabbitMQ:** Consume y procesa los eventos emitidos desde el microservicio de categorías, asegurando la consistencia eventual de datos sin acoplamiento directo entre bases de datos.

### 5. RabbitMQ (Message Broker)
* Servidor de mensajería AMQP que desacopla la comunicación síncrona.
* Incluye el plugin de administración Web (Management Dashboard) accesible en el puerto `15672`.

### 6. SQL Server (Motor de Base de Datos)
* Aloja tres bases de datos relacionales independientes:
  * `Seguridad`: Tabla `Usuarios`.
  * `categoria`: Tabla `categoria`.
  * `vehciulos`: Tabla `vehiculos`.

---

## 🔑 Procedimiento de Autenticación JWT y Pruebas

### Usuarios Semilla Precargados en el Sistema:

| Usuario | Contraseña | Rol Asignado | Privilegios en el Sistema |
| :--- | :--- | :--- | :--- |
| **`admin`** | `Admin123*` | **Administrador** | Acceso total (Lectura, Creación, Modificación, Eliminación) |
| **`usuario`** | `User123*` | **Usuario** | Acceso de solo lectura (Consultas GET) |

---

### Paso 1: Obtener el Token JWT (Login)

**Solicitud HTTP POST:**
```bash
curl -X POST http://158.23.165.154:5260/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123*"
  }'
```

**Ejemplo de Respuesta Exitosa (HTTP 200 OK):**
```json
{
  "exito": true,
  "mensaje": "Inicio de sesión exitoso",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "username": "admin",
  "rol": "Administrador",
  "expiracion": "2026-09-10T07:37:53Z"
}
```

---

### Paso 2: Consumir Endpoints Protegidos con el Token

Copiar el valor del atributo `"token"` y enviarlo en el header `Authorization`:

#### Consultar Categorías:
```bash
curl -X GET http://158.23.165.154:5259/api/Categoria \
  -H "Authorization: Bearer <TU_TOKEN_JWT>"
```

#### Consultar Vehículos:
```bash
curl -X GET http://158.23.165.154:5258/api/Vehiculo \
  -H "Authorization: Bearer <TU_TOKEN_JWT>"
```

#### Probar desde Swagger UI:
1. Abrir la interfaz Swagger del servicio deseado (ej. [http://158.23.165.154:5259/swagger](http://158.23.165.154:5259/swagger)).
2. Hacer clic en el botón verde **"Authorize"** en la esquina superior derecha.
3. En el campo de texto, pegar el token JWT (o `Bearer <TOKEN>`) y presionar **Authorize**.
4. Ejecutar cualquiera de los endpoints protegidos.

---

## 📑 Listado de Endpoints Principales

| Microservicio | Método | Ruta Vía API Gateway | Ruta Directa | Autorización | Descripción |
| :--- | :---: | :--- | :--- | :---: | :--- |
| **Seguridad** | `POST` | `/api/auth/login` | `:5260/api/auth/login` | Público | Autenticación y generación de JWT |
| **Seguridad** | `POST` | `/api/auth/registro` | `:5260/api/auth/registro` | Público | Registro de nuevos usuarios |
| **Seguridad** | `GET` | `/api/auth/perfil` | `:5260/api/auth/perfil` | Bearer JWT | Datos del usuario autenticado |
| **Categoría** | `GET` | `/api/categoria` | `:5259/api/Categoria` | Admin / Usuario | Listar todas las categorías |
| **Categoría** | `GET` | `/api/categoria/{id}` | `:5259/api/Categoria/{id}` | Admin / Usuario | Obtener categoría por ID |
| **Categoría** | `POST` | `/api/categoria` | `:5259/api/Categoria` | Solo Admin | Crear nueva categoría (Publica en RabbitMQ) |
| **Categoría** | `PUT` | `/api/categoria/{id}` | `:5259/api/Categoria/{id}` | Solo Admin | Actualizar categoría (Publica en RabbitMQ) |
| **Categoría** | `DELETE`| `/api/categoria/{id}` | `:5259/api/Categoria/{id}` | Solo Admin | Eliminar categoría |
| **Vehículo** | `GET` | `/api/vehiculo` | `:5258/api/Vehiculo` | Admin / Usuario | Listar todos los vehículos |
| **Vehículo** | `GET` | `/api/vehiculo/{id}` | `:5258/api/Vehiculo/{id}` | Admin / Usuario | Obtener vehículo por ID |
| **Vehículo** | `POST` | `/api/vehiculo` | `:5258/api/Vehiculo` | Solo Admin | Registrar vehículo nuevo |
| **Vehículo** | `PUT` | `/api/vehiculo/{id}` | `:5258/api/Vehiculo/{id}` | Solo Admin | Actualizar datos del vehículo |
| **Vehículo** | `DELETE`| `/api/vehiculo/{id}` | `:5258/api/Vehiculo/{id}` | Solo Admin | Eliminar vehículo |

---

## 💻 Instrucciones para Ejecución Local con Docker Compose

### Requisitos Previos:
* [Docker Desktop](https://www.docker.com/products/docker-desktop) instalado y en ejecución.
* [Git](https://git-scm.com/) instalado en el sistema operativo.

### Pasos de Ejecución:

1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com/jegtvir/DIST-4AM-AD01-DockerCompose-RabbitMQApiGateway-Jonathan-Guaico.git
   cd DIST-4AM-AD01-DockerCompose-RabbitMQApiGateway-Jonathan-Guaico
   ```

2. **Compilar y levantar la arquitectura completa:**
   ```bash
   docker compose up -d --build
   ```

3. **Verificar el estado de los contenedores:**
   ```bash
   docker compose ps
   ```
   Deberán figurar en estado `Up` los siguientes contenedores:
   * `sqlserver` (Base de datos relacional)
   * `sqlserver-init` (Inicializador del login de base de datos)
   * `rabbitmq` (Broker de mensajería)
   * `seguridad-api` (Microservicio de seguridad y tokens)
   * `categoria-api` (Microservicio de categorías)
   * `vehiculo-api` (Microservicio de vehículos)
   * `apigateway` (Gateway de entrada YARP)

4. **Acceso local a las interfaces Swagger:**
   * **API Gateway:** `http://localhost:5119`
   * **Seguridad Swagger:** `http://localhost:5260/swagger`
   * **Categoría Swagger:** `http://localhost:5259/swagger`
   * **Vehículo Swagger:** `http://localhost:5258/swagger`
   * **RabbitMQ Dashboard:** `http://localhost:15672` (User: `admin` / Pass: `admin123`)

5. **Detener la aplicación local:**
   ```bash
   docker compose down
   ```

---

## 🗑️ Instrucciones para Detener y Eliminar Recursos de Azure

Una vez concluida la evaluación docente de la práctica, se recomienda ejecutar los siguientes comandos para evitar el consumo de saldo o facturación en Azure:

### 1. Detener contenedores en el servidor de Azure:
```bash
docker compose down -v
```

### 2. Eliminar por completo el Grupo de Recursos en Azure:
```bash
az group delete \
  --name rg-distribuidas-rabbitmq \
  --yes \
  --no-wait
```
Este comando elimina inmediatamente la máquina virtual, las reglas de red (NSG), la IP pública asignada, el disco de almacenamiento y cualquier recurso dependiente asociado a la práctica.
