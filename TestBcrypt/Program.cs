var dbHash = "$2a$11$zS4jhsOZIuoY.K4nHl7k3O3bCEQcDwWHxGLODZVl/v5Gs3J4J7yYK";
var seedHash = "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq";
var userHash = "$2a$11$q43GcbtmtTn9FyysOC73SO4HUFfBAF43GzPuZ6y0d0EZeDitCKqGa";

var passwords = new[] { "admin123", "ventas123", "optometra123", "user123", "Admin123", "admin", "123456" };

Console.WriteLine("--- EVALUANDO HASH EN BASE DE DATOS (optometra, ventas, admin) ---");
foreach (var pass in passwords)
{
    if (BCrypt.Net.BCrypt.Verify(pass, dbHash))
        Console.WriteLine($"[BD HASH MATCH] -> Password es: '{pass}'");
}

Console.WriteLine("\n--- EVALUANDO HASH EN SEED DE DBCONTEXT (admin123) ---");
foreach (var pass in passwords)
{
    if (BCrypt.Net.BCrypt.Verify(pass, seedHash))
        Console.WriteLine($"[SEED HASH MATCH] -> Password es: '{pass}'");
}

Console.WriteLine("\n--- EVALUANDO HASH EN SEED DE USUARIO COMUN (user123) ---");
foreach (var pass in passwords)
{
    if (BCrypt.Net.BCrypt.Verify(pass, userHash))
        Console.WriteLine($"[USER HASH MATCH] -> Password es: '{pass}'");
}
