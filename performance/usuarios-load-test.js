import http from 'k6/http';
import { check, sleep } from 'k6';

// Variables de entorno para configuracion de la prueba de carga
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || '';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || '';
const USER_EMAIL = __ENV.USER_EMAIL || '';
const USER_PASSWORD = __ENV.USER_PASSWORD || '';

// Nota de diseno y arquitectura de seguridad:
// BCrypt consume CPU a proposito durante el login (work factor / cost deliberate)
// para proteger las contrasenas contra ataques de fuerza bruta y diccionarios offline.
// Es una decision explicita de seguridad, por lo que el umbral de latencia p(95) para
// el escenario de autenticacion (api_auth) contempla este costo criptografico esperado.
export const options = {
    scenarios: {
        api_auth: {
            executor: 'ramping-vus',
            startVUs: 5,
            stages: [
                { duration: '1m', target: 20 },
                { duration: '1m', target: 20 },
            ],
            exec: 'apiAuthScenario',
            tags: { scenario: 'api_auth' },
        },
        admin_listados: {
            executor: 'constant-vus',
            vus: 5,
            duration: '1m',
            exec: 'adminListadosScenario',
            tags: { scenario: 'admin_listados' },
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'],
        'http_req_duration{scenario:api_auth}': ['p(95)<1000'],
        'http_req_duration{scenario:admin_listados}': ['p(95)<1500'],
    },
};

// Escenario 1: Autenticacion API y consulta de perfil
export function apiAuthScenario() {
    const loginPayload = JSON.stringify({
        email: USER_EMAIL,
        password: USER_PASSWORD,
    });

    const loginRes = http.post(
        `${BASE_URL}/api/auth/login`,
        loginPayload,
        {
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
            },
            tags: { scenario: 'api_auth' },
        }
    );

    const loginOk = check(loginRes, {
        'api login status is 200': (r) => r.status === 200,
        'api login token presente': (r) => {
            try {
                const body = JSON.parse(r.body);
                return !!body.token;
            } catch (_) {
                return false;
            }
        },
    });

    if (loginOk) {
        let token = '';
        try {
            token = JSON.parse(loginRes.body).token;
        } catch (_) {}

        if (token) {
            const meRes = http.get(
                `${BASE_URL}/api/users/me`,
                {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Accept': 'application/json',
                    },
                    tags: { scenario: 'api_auth' },
                }
            );

            check(meRes, {
                'api me status is 200': (r) => r.status === 200,
            });
        }
    }

    sleep(1);
}

// Estado de sesion web aislado por cada VU
let webLoggedIn = false;

// Escenario 2: Listados de administracion de clientes y trabajadores con paginacion
export function adminListadosScenario() {
    if (!webLoggedIn) {
        const loginPageRes = http.get(`${BASE_URL}/Auth/Login`, {
            tags: { scenario: 'admin_listados' },
        });

        check(loginPageRes, {
            'login page status is 200': (r) => r.status === 200,
        });

        const doc = loginPageRes.html();
        const verificationToken = doc.find('input[name="__RequestVerificationToken"]').attr('value') || '';

        const loginPostRes = http.post(
            `${BASE_URL}/Auth/Login`,
            {
                Email: ADMIN_EMAIL,
                Password: ADMIN_PASSWORD,
                __RequestVerificationToken: verificationToken,
            },
            {
                tags: { scenario: 'admin_listados' },
            }
        );

        check(loginPostRes, {
            'admin login web exitoso': (r) => r.status === 200 || r.status === 302,
        });

        webLoggedIn = true;
    }

    // Consulta de listado de clientes
    const clientesRes = http.get(
        `${BASE_URL}/Dashboard/Admin?section=usuarios&subtab=clientes`,
        {
            tags: { scenario: 'admin_listados' },
        }
    );

    check(clientesRes, {
        'clientes listado status is 200': (r) => r.status === 200,
    });

    sleep(1);

    // Consulta de listado de trabajadores con tamano de pagina 20
    const trabajadoresRes = http.get(
        `${BASE_URL}/Dashboard/Admin?section=usuarios&subtab=trabajadores&trabajadoresPageSize=20`,
        {
            tags: { scenario: 'admin_listados' },
        }
    );

    check(trabajadoresRes, {
        'trabajadores listado status is 200': (r) => r.status === 200,
    });

    sleep(1);
}
