import http from 'k6/http';
import { check, sleep } from 'k6';

// Variables de entorno para configuracion de la prueba de carga del modulo Optometra
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const OPTOMETRA_EMAIL = __ENV.OPTOMETRA_EMAIL || 'optometra@lentsoft.com';
const OPTOMETRA_PASSWORD = __ENV.OPTOMETRA_PASSWORD || 'admin123';

export const options = {
    scenarios: {
        optometra_dashboard: {
            executor: 'ramping-vus',
            startVUs: 5,
            stages: [
                { duration: '1m', target: 20 }, // Rampa de 5 a 20 usuarios virtuales concurrentes
                { duration: '1m', target: 20 }, // Mantenimiento en 20 VUs
            ],
            exec: 'optometraDashboardScenario',
            tags: { scenario: 'optometra_dashboard' },
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'],
        'http_req_duration{scenario:optometra_dashboard}': ['p(95)<2000'], // Umbral p95 menor a 2 segundos
    },
};

// Estado de sesion web aislado por cada VU
let webLoggedIn = false;

// Escenario: Login web con token antiforgery y consulta recurrente de Citas y Pacientes
export function optometraDashboardScenario() {
    if (!webLoggedIn) {
        // 1. Obtener formulario de login y extraer token antiforgery
        const loginPageRes = http.get(`${BASE_URL}/Auth/Login`, {
            tags: { scenario: 'optometra_dashboard' },
        });

        check(loginPageRes, {
            'login page status is 200': (r) => r.status === 200,
        });

        const doc = loginPageRes.html();
        const verificationToken = doc.find('input[name="__RequestVerificationToken"]').attr('value') || '';

        // 2. Iniciar sesion como optometra
        const loginPostRes = http.post(
            `${BASE_URL}/Auth/Login`,
            {
                Email: OPTOMETRA_EMAIL,
                Password: OPTOMETRA_PASSWORD,
                __RequestVerificationToken: verificationToken,
            },
            {
                tags: { scenario: 'optometra_dashboard' },
            }
        );

        check(loginPostRes, {
            'optometra login web exitoso': (r) => r.status === 200 || r.status === 302,
        });

        webLoggedIn = true;
    }

    // 3. Consulta recurrente de la seccion de Citas
    const citasRes = http.get(
        `${BASE_URL}/Optometra?section=citas`,
        {
            tags: { scenario: 'optometra_dashboard' },
        }
    );

    check(citasRes, {
        'seccion citas status is 200': (r) => r.status === 200,
    });

    sleep(1);

    // 4. Consulta recurrente de la seccion de Pacientes
    const pacientesRes = http.get(
        `${BASE_URL}/Optometra?section=pacientes`,
        {
            tags: { scenario: 'optometra_dashboard' },
        }
    );

    check(pacientesRes, {
        'seccion pacientes status is 200': (r) => r.status === 200,
    });

    sleep(1);
}

