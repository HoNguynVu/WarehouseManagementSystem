# Warehouse Management System (Microservices)

Đây là dự án hệ thống Quản lý Kho Hàng & Bán Hàng trực tuyến được xây dựng theo kiến trúc **Microservices** hiện đại, kết hợp với các Design Pattern tiên tiến như **CQRS**, **Event-Driven Architecture** (Pub/Sub) và **API Gateway**.

---

## 🚀 Công Nghệ Sử Dụng

### 1. Backend & Framework
- **.NET 8 (C#)**: Nền tảng cốt lõi xây dựng các Microservices.
- **ASP.NET Core Web API**: Xây dựng RESTful API.
- **Entity Framework Core**: ORM tương tác với cơ sở dữ liệu.
- **MediatR**: Triển khai CQRS Pattern (tách biệt Command và Query).
- **AutoMapper**: Ánh xạ dữ liệu giữa Entity và DTO.
- **FluentValidation**: Xác thực dữ liệu đầu vào.

### 2. Microservices Architecture & Infrastructure
- **YARP (Yet Another Reverse Proxy)**: Đóng vai trò làm **API Gateway**, định tuyến request từ Client tới các Services.
- **Polly**: Quản lý Resiliency (khả năng phục hồi), cấu hình Retry, Circuit Breaker, Timeout.
- **RabbitMQ**: Đóng vai trò là Message Broker / Event Bus để các Services giao tiếp bất đồng bộ (Ví dụ: OrderService gửi Message để WarehouseService trừ kho).
- **Redis**: Phân tán bộ nhớ đệm (Distributed Cache).
- **Seq**: Thu thập Log tập trung (Centralized Logging).
- **HealthChecks UI**: Bảng điều khiển giám sát sức khỏe của toàn bộ hệ thống container.

### 3. Databases (Polyglot Persistence)
Mỗi service quản lý một database riêng biệt, tối ưu theo từng mục đích:
- **SQL Server**: Dành cho `IdentityService` và `OrderService` (Dữ liệu quan hệ chặt chẽ).
- **PostgreSQL**: Dành cho `WarehouseService`.
- **MongoDB**: Dành cho `CatalogService` (Dữ liệu NoSQL linh hoạt, phi cấu trúc).

### 4. Containerization & Deployment
- **Docker**: Đóng gói các Microservices.
- **Docker Compose**: Điều phối toàn bộ hạ tầng mạng (Infrastructure) và ứng dụng (App) chỉ với 1 lệnh khởi chạy.

---

## 🏗️ Cấu Trúc Dự Án (Domain-Driven Design)

Toàn bộ hệ thống được chia thành 5 Microservices chính:
1. **ApiGateway**: Cổng giao tiếp duy nhất ra bên ngoài, tích hợp Rate Limiting, CORS, và Resiliency (Polly).
2. **IdentityService**: Quản lý tài khoản, đăng nhập, và cấp phát JWT Token.
3. **CatalogService**: Quản lý thông tin và danh mục sản phẩm (Sử dụng MongoDB).
4. **OrderService**: Quản lý đặt hàng, thanh toán ZaloPay Sandbox, trạng thái đơn hàng (CQRS & Saga Pattern).
5. **WarehouseService**: Quản lý nhập/xuất kho, kiểm tra tồn kho. Lắng nghe Event từ OrderService để trừ tồn kho.

Mỗi service (ngoại trừ Gateway) đều tuân theo kiến trúc Clean Architecture / DDD, bao gồm 4 lớp:
- `API`: Chứa Controllers.
- `Application`: Chứa Use Cases (CQRS Commands/Queries), DTOs, Interfaces.
- `Domain`: Chứa Entities gốc, Enums, Exceptions.
- `Infrastructure`: Chứa Database Context, Repositories, Event Bus Consumers.

Bên cạnh đó, có một dự án `SharedLibrary` chứa các Event Models và Common Components được dùng chung.

---

## ⚙️ Hướng Dẫn Cài Đặt & Chạy Dự Án

Dự án đã được tự động hóa hoàn toàn bằng Docker Compose. Đảm bảo máy tính của bạn đã cài đặt **Docker Desktop**.

### Bước 1: Khởi động hệ thống
Mở Terminal (PowerShell / CMD) tại thư mục gốc của dự án (nơi chứa file `docker-compose.yaml`) và chạy lệnh:
```bash
docker-compose up -d --build
```
*Lưu ý: Quá trình build lần đầu tiên có thể mất vài phút để tải các Images cần thiết.*

### Bước 2: Kiểm tra sức khỏe hệ thống (Health Check)
Truy cập vào Dashboard để đảm bảo tất cả Database và Services đều xanh (Healthy):
👉 **http://localhost:5000/healthchecks-ui**

### Bước 3: Truy cập hệ thống & Các cổng mạng
Toàn bộ request từ phía Client (Frontend) sẽ đều phải đi qua **API Gateway ở cổng `5000`**.

- **API Gateway (Main Entry Point):** `http://localhost:5000`
- **Identity API:** `http://localhost:5000/api/Auth`
- **Catalog API:** `http://localhost:5000/api/Catalog`
- **Order API:** `http://localhost:5000/api/Order`
- **Warehouse API:** `http://localhost:5000/api/Warehouse`

### Bước 4: Giám sát Hệ thống (Logging & RabbitMQ)
- **Hệ thống Log tập trung (Seq):** `http://localhost:5341`
- **Quản lý RabbitMQ:** `http://localhost:15672` (Tài khoản: `guest` / Mật khẩu: `guest`)

