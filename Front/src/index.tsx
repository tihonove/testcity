import * as React from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { App } from "./App";
import { TestAnalyticsThemeProvider } from "./Theme/TestAnalyticsThemeProvider";

import { Suspense } from "react";
import "./CommonStyles/Fonts.css";
import { GlobalStyle } from "./CommonStyles/GlobalStyle";
import "./CommonStyles/Reset.css";
import { Loader } from "./Components/Loader";
import { reject } from "./TypeHelpers";

const root = createRoot(document.getElementById("root") ?? reject("Not found #root element"));

root.render(
    <React.StrictMode>
        <TestAnalyticsThemeProvider>
            <BrowserRouter>
                <GlobalStyle />
                <Suspense fallback={<Loader />}>
                    <App />
                </Suspense>
            </BrowserRouter>
        </TestAnalyticsThemeProvider>
    </React.StrictMode>
);
