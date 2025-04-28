import * as React from "react";
import { createRoot } from "react-dom/client";

import { reject } from "./Utils/TypeHelpers";
import { AppBootstrap } from "./AppBootstrap";
import "./CommonStyles/Theme.css";
import "./CommonStyles/Fonts.css";
import "./CommonStyles/Reset.css";
import "./CommonStyles/GlobalStyle.css";

const root = createRoot(document.getElementById("root") ?? reject("Not found #root element"));

root.render(<AppBootstrap />);
