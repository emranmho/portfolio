---
name: ClothStore
summary: "Full e-commerce platform with 4-role RBAC, dual-mode cart, and public order tracking."
stack: [dotnet, postgresql, efcore, docker, opentelemetry]
repoUrl:
liveUrl:
featured: false
order: 4
---

A .NET 10 MVC e-commerce platform with four distinct roles (customer, staff, manager, admin), a cart that works for both guests and authenticated users, and public order tracking without requiring an account. Observability wired in from the start with Serilog + Seq + OpenTelemetry, PDF invoicing via QuestPDF, transactional email via MailKit, and xUnit coverage across the checkout flow.
