//1. Crear el servidor y los datos 
//using Microsoft.Win32;

var app = WebApplication.CreateBuilder(args).Build(); //Crear el servidor

//2. Listas en memoria: son nuestras "bases de datos" de practica
// al apagar la API, se borran (la cuenta de prueba siempre esta disponible)
var usuarios = new List<Usuario>
{
    new("prueba@practica.com","123456", "usuario de prueba")
};

var perfiles = new Dictionary<string, Perfil>(); //Correo -> su perfil guardado

//Ventanilla de prueba: GET a la direccion raiz
app.MapGet("/", () => "API de practica funcionando");

//2. Ventanilla de registro
//POST /api/auth/registro: crear una cuenta nueva
//(registro r) = los dato JSON que manda la app, ya convertidos a objeto
app.MapPost("/api/auth/registro", (Registro r) =>
{
    //Quitar espacios y pasa a minusculas, para que un Ana@x.com sea igual a ana@x.com
    var correo = r.Correo.Trim().ToLower();

    //Validacion 1: El correo debe tener arroba. Responde 400 (Pedido mal hecho)
    if (!correo.Contains("@"))
    {
        return Results.BadRequest(new { mensaje = "El correo debe tener un @" });
    }
    //Validacion 2: La contraseña debe tener al menos 6 caracteres
    if (r.Clave.Length < 6)
    {
        return Results.BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });
    }
    //Validacion 3: No puede haber dos cuentas con el mismo correo. Responde 400.
    if (usuarios.Any(u => u.Correo == correo))
    {
        return Results.BadRequest(new { mensaje = "Ya existe una cuenta con ese correo" });
    }
    usuarios.Add(new(correo, r.Clave, r.Nombre.Trim())); //Guarda la cuenta
    return Results.Ok(new { mensaje = "Cuenta creada exitosamente" }); // 200. Todo salio bien
});

//3. Ventanilla de login
//POST /api/auth/login: revisa los datos y entrega un token(el pase)
app.MapPost("/api/auth/login", (Login l) =>
{
    var correo = l.Correo.Trim().ToLower();
    //True solo si hay un usuario con ese correo y esa contraseña. Si no, null
    var ok = usuarios.FirstOrDefault(u => u.Correo == correo && u.Clave == l.Clave);

    return ok is null
        ? Results.Ok(new { token = correo })
        : Results.Json(new { mensaje = "Datos incorrectos" },
                        statusCode: 401); // no: 401 = no es autorizado
});

//4. Ventanilla de perfil (GET)
//GET /api/perfil: entrega el perfil del usuario que manda el token
app.MapGet("/api/perfil", (HttpRequest req) =>
{
    var correo = Quien(req); //Leemos el token de la peticion

    //Buscamos al usuario que tiene ese correo. Si no existe: es un pase falso. Responde 401 (no autorizado)
    var u = usuarios.FirstOrDefault(x => x.Correo == correo);

    return u is null
        ? Results.Json(new { mensaje = "Usuario no encontrado" }, statusCode: 401)
        : Results.Ok(perfiles.GetValueOrDefault(correo, new Perfil(u.nombre, true, 0)));
});

//5. Ventanilla de perfil (PUT)
//PUT /api/perfil: guarda el perfil del usuario que manda el token
app.MapPut("/api/perfil", (HttpRequest req, Perfil p) =>
{
    var correo = Quien(req); //Leemos el token de la peticion
    //Buscamos al usuario que tiene ese correo. Si no existe: es un pase falso. Responde 401 (no autorizado)
    var u = usuarios.FirstOrDefault(x => x.Correo == correo);
    if (u is null)
    {
        return Results.Json(new { mensaje = "Usuario no encontrado" }, statusCode: 401);
    }
    perfiles[correo] = p; //Guarda el perfil del usuario
    return Results.Ok(new { mensaje = "Perfil guardado exitosamente" });
});

static string Quien(HttpRequest req) =>
    req.Headers.Authorization.ToString().Replace("Bearer ", "").Trim();

app.Run(); //Arranca el servidor

// "Cajas" de datos (DTO): la forma del JSON que viaja entre la app y la API
// Van al final del archivo
record Usuario(string Correo, string Clave, string nombre);
record Registro(string Correo, string Clave, string Nombre);
record Login(string Correo, string Clave);
record Perfil(string Nombre, bool Disponible, int AniosExperiencia);