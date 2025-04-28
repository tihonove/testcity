import * as React from "react";
import { Suspense } from "react";
import { BrowserRouter } from "react-router-dom";
import { App } from "./App";
import { PageLoader } from "./Components/PageLoader";
import { TestAnalyticsThemeProvider } from "./Theme/TestAnalyticsThemeProvider";

export function AppBootstrap() {
    return (
        <React.StrictMode>
            <TestAnalyticsThemeProvider>
                <BrowserRouter future={{ v7_relativeSplatPath: true, v7_startTransition: true }}>
                    <Suspense fallback={<PageLoader />}>
                        <App />
                    </Suspense>
                </BrowserRouter>
            </TestAnalyticsThemeProvider>
        </React.StrictMode>
    );
}
