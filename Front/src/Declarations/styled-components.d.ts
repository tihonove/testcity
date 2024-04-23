import "styled-components";
import { ITheme } from "../Theme/ITheme";

declare module "styled-components" {
    export interface DefaultTheme extends ITheme {}
}
