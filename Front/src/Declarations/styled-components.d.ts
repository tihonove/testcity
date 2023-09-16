import type { CSSProp } from "styled-components";
import {ITheme} from "../Theme/ITheme";

declare module "styled-components" {
    export interface DefaultTheme extends ITheme {}
}

declare module "react" {
    interface DOMAttributes<T> {
        css?: CSSProp;
    }
}