using Microsoft.EntityFrameworkCore;
using PKS_sem4_kr4_2.Models;

namespace PKS_sem4_kr4_2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductionLine> ProductionLines { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<ProductMaterial> ProductMaterials { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка составного ключа для связи многие-ко-многим
            modelBuilder.Entity<ProductMaterial>()
                .HasKey(pm => new { pm.ProductId, pm.MaterialId });

            modelBuilder.Entity<ProductMaterial>()
                .HasOne(pm => pm.Product)
                .WithMany(p => p.ProductMaterials)
                .HasForeignKey(pm => pm.ProductId);

            modelBuilder.Entity<ProductMaterial>()
                .HasOne(pm => pm.Material)
                .WithMany(m => m.ProductMaterials)
                .HasForeignKey(pm => pm.MaterialId);

            // Настройка связи WorkOrder и ProductionLine
            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.ProductionLine)
                .WithMany(l => l.WorkOrders)
                .HasForeignKey(w => w.ProductionLineId);

            // Заполнение начальными данными (для тестирования)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // ========================================
            // МАТЕРИАЛЫ
            // ========================================
            modelBuilder.Entity<Material>().HasData(
                new Material { Id = 1, Name = "Стальной лист", Quantity = 500, UnitOfMeasure = "кг", MinimalStock = 100 },
                new Material { Id = 2, Name = "Алюминиевый профиль", Quantity = 200, UnitOfMeasure = "м", MinimalStock = 50 },
                new Material { Id = 3, Name = "Пластиковые гранулы", Quantity = 45, UnitOfMeasure = "кг", MinimalStock = 100 }, // Низкий запас!
                new Material { Id = 4, Name = "Электронные компоненты", Quantity = 15, UnitOfMeasure = "шт", MinimalStock = 20 }, // Низкий запас!
                new Material { Id = 5, Name = "Крепёжные болты", Quantity = 5000, UnitOfMeasure = "шт", MinimalStock = 1000 },
                new Material { Id = 6, Name = "Краска порошковая", Quantity = 80, UnitOfMeasure = "кг", MinimalStock = 50 },
                new Material { Id = 7, Name = "Упаковочная плёнка", Quantity = 300, UnitOfMeasure = "м", MinimalStock = 100 },
                new Material { Id = 8, Name = "Медная проволока", Quantity = 60, UnitOfMeasure = "кг", MinimalStock = 75 } // Низкий запас!
            );

            // ========================================
            // ПРОДУКТЫ
            // ========================================
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Металлический корпус",
                    Description = "Стандартный корпус для электрооборудования",
                    SpecificationsJson = "{\"Вес\":\"5 кг\",\"Размер\":\"400x300x200 мм\",\"Материал\":\"Сталь\"}",
                    Category = "Корпуса",
                    MinimalStock = 10,
                    ProductionTimePerUnit = 30
                },
                new Product
                {
                    Id = 2,
                    Name = "Алюминиевая рама",
                    Description = "Рама для промышленного конвейера",
                    SpecificationsJson = "{\"Вес\":\"3 кг\",\"Длина\":\"2 м\",\"Материал\":\"Алюминий\"}",
                    Category = "Рамы",
                    MinimalStock = 5,
                    ProductionTimePerUnit = 20
                },
                new Product
                {
                    Id = 3,
                    Name = "Пластиковый корпус",
                    Description = "Лёгкий корпус для бытовой электроники",
                    SpecificationsJson = "{\"Вес\":\"0.5 кг\",\"Размер\":\"200x150x100 мм\",\"Цвет\":\"Белый\"}",
                    Category = "Корпуса",
                    MinimalStock = 20,
                    ProductionTimePerUnit = 15
                },
                new Product
                {
                    Id = 4,
                    Name = "Пульт управления",
                    Description = "Электронный пульт для станков ЧПУ",
                    SpecificationsJson = "{\"Вес\":\"1.2 кг\",\"Экран\":\"7 дюймов\",\"Интерфейс\":\"USB/WiFi\"}",
                    Category = "Электроника",
                    MinimalStock = 8,
                    ProductionTimePerUnit = 45
                }
            );

            // ========================================
            // СВЯЗИ ПРОДУКТОВ И МАТЕРИАЛОВ
            // ========================================
            modelBuilder.Entity<ProductMaterial>().HasData(
                // Металлический корпус: сталь + крепёж + краска
                new ProductMaterial { ProductId = 1, MaterialId = 1, QuantityNeeded = 5.0m },  // 5 кг стали
                new ProductMaterial { ProductId = 1, MaterialId = 5, QuantityNeeded = 20.0m }, // 20 болтов
                new ProductMaterial { ProductId = 1, MaterialId = 6, QuantityNeeded = 0.5m },   // 0.5 кг краски

                // Алюминиевая рама: алюминий + крепёж
                new ProductMaterial { ProductId = 2, MaterialId = 2, QuantityNeeded = 2.5m },   // 2.5 м алюминия
                new ProductMaterial { ProductId = 2, MaterialId = 5, QuantityNeeded = 10.0m },  // 10 болтов

                // Пластиковый корпус: пластик + упаковка
                new ProductMaterial { ProductId = 3, MaterialId = 3, QuantityNeeded = 0.5m },   // 0.5 кг пластика
                new ProductMaterial { ProductId = 3, MaterialId = 7, QuantityNeeded = 1.0m },    // 1 м упаковки

                // Пульт управления: электроника + медь + упаковка
                new ProductMaterial { ProductId = 4, MaterialId = 4, QuantityNeeded = 3.0m },    // 3 компонента
                new ProductMaterial { ProductId = 4, MaterialId = 8, QuantityNeeded = 0.2m },    // 0.2 кг меди
                new ProductMaterial { ProductId = 4, MaterialId = 7, QuantityNeeded = 0.5m }     // 0.5 м упаковки
            );

            // ========================================
            // ПРОИЗВОДСТВЕННЫЕ ЛИНИИ
            // ========================================
            modelBuilder.Entity<ProductionLine>().HasData(
                new ProductionLine { Id = 1, Name = "Линия сборки №1", Status = "Active", EfficiencyFactor = 1.0f },
                new ProductionLine { Id = 2, Name = "Линия сборки №2", Status = "Active", EfficiencyFactor = 0.8f },
                new ProductionLine { Id = 3, Name = "Линия покраски", Status = "Active", EfficiencyFactor = 1.2f },
                new ProductionLine { Id = 4, Name = "Сборочный конвейер А", Status = "Stopped", EfficiencyFactor = 1.0f },
                new ProductionLine { Id = 5, Name = "Упаковочная линия", Status = "Active", EfficiencyFactor = 1.5f }
            );

            // ========================================
            // ТЕСТОВЫЕ ЗАКАЗЫ
            // ========================================
            modelBuilder.Entity<WorkOrder>().HasData(
                new WorkOrder
                {
                    Id = 1,
                    ProductId = 1,
                    ProductionLineId = 1,
                    Quantity = 50,
                    StartDate = DateTime.Now.AddHours(-5),
                    EstimatedEndDate = DateTime.Now.AddHours(20),
                    Status = "InProgress",
                    ProgressPercent = 45
                },
                new WorkOrder
                {
                    Id = 2,
                    ProductId = 3,
                    ProductionLineId = 3,
                    Quantity = 100,
                    StartDate = DateTime.Now.AddDays(1),
                    EstimatedEndDate = DateTime.Now.AddDays(3),
                    Status = "Pending",
                    ProgressPercent = 0
                },
                new WorkOrder
                {
                    Id = 3,
                    ProductId = 2,
                    Quantity = 20,
                    StartDate = DateTime.Now.AddDays(-1),
                    EstimatedEndDate = DateTime.Now.AddHours(5),
                    Status = "Pending",
                    ProgressPercent = 0
                },
                new WorkOrder
                {
                    Id = 4,
                    ProductId = 4,
                    ProductionLineId = 2,
                    Quantity = 30,
                    StartDate = DateTime.Now.AddHours(-2),
                    EstimatedEndDate = DateTime.Now.AddHours(20),
                    Status = "InProgress",
                    ProgressPercent = 15
                },
                new WorkOrder
                {
                    Id = 5,
                    ProductId = 1,
                    Quantity = 75,
                    StartDate = DateTime.Now.AddDays(-2),
                    EstimatedEndDate = DateTime.Now.AddHours(-3),
                    Status = "Completed",
                    ProgressPercent = 100,
                    ActualEndDate = DateTime.Now.AddHours(-4)
                }
            );
        }
    }
}