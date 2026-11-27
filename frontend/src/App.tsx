import { useEffect, useState } from 'react';

interface HealthStatus {
  status: string;
  service?: string;
}

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api';

export function App() {
  const [apiState, setApiState] = useState<string>('Verificando conexión con API...');

  useEffect(() => {
    const controller = new AbortController();
    const healthUrl = API_BASE.replace(/\/api\/?$/, '') + '/health';

    fetch(healthUrl, { signal: controller.signal })
      .then(async (res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = (await res.json()) as HealthStatus;
        setApiState(`Conectado (${data.status})`);
      })
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') return;
        setApiState('API no disponible en este momento (inicia el backend en localhost:5080)');
      });

    return () => controller.abort();
  }, []);

  return (
    <div className="app-shell">
      <header className="topbar">
        <h1>AXIZ GEO</h1>
        <span>Tecnología que orienta</span>
      </header>

      <main className="content">
        <section className="card">
          <h2>Panel de Monitoreo y Geocercas</h2>
          <p>
            Base inicial de la SPA en <strong>React 19 + TypeScript</strong> lista para desarrollo modular.
          </p>
          <p>
            <strong>Estado del Backend (.NET 10):</strong> {apiState}
          </p>
        </section>
      </main>
    </div>
  );
}

export default App;
