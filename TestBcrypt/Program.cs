var users = new[]
{
    ("admin@lentsoft.com",     "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq"),
    ("user@lentsoft.com",      "$2a$11$q43GcbtmtTn9FyysOC73SO4HUFfBAF43GzPuZ6y0d0EZeDitCKqGa"),
    ("optometra@lentsoft.com", "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq"),
    ("ventas@lentsoft.com",    "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq"),
};
var passwords = new[] { "user123", "admin123", "Admin123", "optometra123", "ventas123", "lentsoft123" };

Console.WriteLine($"{"Email",-30} {"Password",-15} {"Match"}");
Console.WriteLine(new string('-', 60));
foreach (var (email, hash) in users)
    foreach (var pass in passwords)
        if (BCrypt.Net.BCrypt.Verify(pass, hash))
            Console.WriteLine($"{email,-30} {pass,-15} ✅ CORRECTO");
