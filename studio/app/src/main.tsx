import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { setupMonaco } from "./lib/monaco";
import App from "./App";
import "maplibre-gl/dist/maplibre-gl.css";
import "./styles.css";

setupMonaco();

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Root element #root not found");

createRoot(rootEl).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
