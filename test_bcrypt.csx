using BCrypt.Net;
var hash = @"/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq";
var passwords = new[] { "user123", "admin123", "Admin123", "lentsoft123", "admin", "123456" };
foreach (var p in passwords)
{
    bool ok = BCrypt.Net.BCrypt.Verify(p, hash);
    Console.WriteLine($"{p}: {(ok ? "CORRECTO" : "incorrecto")}");
}
