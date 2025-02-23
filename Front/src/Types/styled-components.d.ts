import "styled-components";
import { ITheme } from "../Theme/ITheme";

declare module "styled-components" {
    // eslint-disable-next-line @typescript-eslint/no-empty-object-type
    export interface DefaultTheme extends ITheme {}
}
