# Online Store QA

Минимальный интернет-магазин на **.NET 10** + **PostgreSQL** для практики API-тестов и unit-тестов.

## Стек

- ASP.NET Core Web API (.NET 10)
- EF Core + Npgsql
- PostgreSQL 16 (Docker, порт **5435**)
- Scalar UI для просмотра OpenAPI
- xUnit (пустой проект — тесты пишешь сам)

## Структура

```
OnlineStore.sln
├── src/OnlineStore.Api
├── src/OnlineStore.Application
├── src/OnlineStore.Domain
├── src/OnlineStore.Infrastructure
└── tests/OnlineStore.UnitTests
```

## Быстрый старт

### 1. Поднять PostgreSQL

```bash
docker compose up -d
```

БД слушает **localhost:5435** (не 5432/5433 — они часто заняты).

| Параметр | Значение |
|----------|----------|
| Host | localhost |
| Port | 5435 |
| Database | online_store |
| Username | store |
| Password | store_dev_pass |

### 2. Запустить API

```bash
dotnet run --project src/OnlineStore.Api
```

- API: http://localhost:5080  
- Scalar (OpenAPI UI): http://localhost:5080/scalar  
- OpenAPI JSON: http://localhost:5080/openapi/v1.json  

При старте применяются миграции и seed-товары.

### 3. Тесты (когда будешь готов)

```bash
dotnet test
```

## API

### Products

| Method | Path | Описание |
|--------|------|----------|
| GET | `/api/products` | Список (`category`, `minPrice`, `maxPrice`, `onlyActive`) |
| GET | `/api/products/{id}` | По id |
| POST | `/api/products` | Создать |
| PUT | `/api/products/{id}` | Полная замена карточки |
| PATCH | `/api/products/{id}` | Частичное обновление (без stock) |
| PATCH | `/api/products/{id}/stock` | Остаток: `Set` / `Increase` / `Decrease` |
| DELETE | `/api/products/{id}` | Soft-delete (`IsActive = false`) |

**PATCH product** — только переданные поля:

```json
{ "price": 24.99 }
```

**PATCH stock:**

```json
{ "operation": "Increase", "quantity": 20 }
{ "operation": "Decrease", "quantity": 5 }
{ "operation": "Set", "quantity": 100 }
```

### Cart

| Method | Path | Описание |
|--------|------|----------|
| POST | `/api/cart` | Создать корзину |
| GET | `/api/cart/{cartId}` | Получить |
| DELETE | `/api/cart/{cartId}` | Удалить корзину (и все позиции) |
| POST | `/api/cart/{cartId}/items` | Добавить один или несколько товаров |
| PUT | `/api/cart/{cartId}/items/{productId}` | Изменить qty |
| DELETE | `/api/cart/{cartId}/items/{productId}` | Удалить позицию |

Добавление товаров — body всегда массив `items` (один элемент или несколько):

```json
{
  "items": [
    { "productId": "11111111-1111-1111-1111-111111111111", "quantity": 2 },
    { "productId": "33333333-3333-3333-3333-333333333333", "quantity": 1 }
  ]
}
```

Одинаковые `productId` в одном запросе суммируются. Повторный add того же товара увеличивает qty.

### Orders

| Method | Path | Описание |
|--------|------|----------|
| POST | `/api/orders` | Checkout из корзины `{ "cartId": "..." }` |
| GET | `/api/orders` | Список |
| GET | `/api/orders/{id}` | По id |
| PATCH | `/api/orders/{id}/status` | `{ "status": "Paid" }` |

`OrderStatus`: `Pending`, `Paid`, `Shipped`, `Cancelled`

## Бизнес-правила (для тестов)

1. Цена товара > 0, stock ≥ 0  
2. Нельзя добавить в корзину больше, чем stock  
3. Нельзя добавить неактивный товар  
4. Checkout пустой корзины → 400  
5. Checkout уменьшает stock и очищает корзину  
6. Отмена (`Cancelled`) возвращает stock  
7. Нельзя отменить уже `Shipped` заказ  
8. PATCH product без полей → 400  
9. Stock `Decrease` больше текущего остатка → 400  
10. Stock `Increase`/`Decrease` с quantity = 0 → 400  

Ошибки: `application/problem+json`  
- `404` — NotFound  
- `400` — Business rule  

## Seed products

| Id | Name | Category | Price | Active |
|----|------|----------|-------|--------|
| `11111111-1111-1111-1111-111111111111` | Wireless Mouse | Electronics | 29.99 | yes |
| `22222222-2222-2222-2222-222222222222` | Mechanical Keyboard | Electronics | 89.99 | yes |
| `33333333-3333-3333-3333-333333333333` | Coffee Mug | Home | 12.50 | yes |
| `44444444-4444-4444-4444-444444444444` | Notebook A5 | Office | 5.99 | yes |
| `55555555-5555-5555-5555-555555555555` | Discontinued Headphones | Electronics | 49.99 | **no** |

## Пример сценария

```bash
# создать корзину
curl -X POST http://localhost:5080/api/cart

# добавить товары (один или несколько)
curl -X POST http://localhost:5080/api/cart/{cartId}/items ^
  -H "Content-Type: application/json" ^
  -d "{\"items\":[{\"productId\":\"11111111-1111-1111-1111-111111111111\",\"quantity\":2},{\"productId\":\"33333333-3333-3333-3333-333333333333\",\"quantity\":1}]}"

# удалить корзину
curl -X DELETE http://localhost:5080/api/cart/{cartId}

# оформить заказ
curl -X POST http://localhost:5080/api/orders ^
  -H "Content-Type: application/json" ^
  -d "{\"cartId\":\"{cartId}\"}"
```
