import * as React from "react";
import {createRoot} from "react-dom/client";
import {App} from "./App";
import {BrowserRouter} from "react-router-dom";

import "./CommonStyles/Fonts.css";
import "./CommonStyles/Reset.css";
import {ThemeContext, THEME_2022} from "@skbkontur/react-ui";

const root = createRoot(document.getElementById('root')!);
root.render(
    <React.StrictMode>
        <BrowserRouter>
            <ThemeContext.Provider value={THEME_2022}>
                <App/>
            </ThemeContext.Provider>
        </BrowserRouter>
    </React.StrictMode>
);

