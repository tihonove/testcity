import { DARK_THEME, LIGHT_THEME } from "@skbkontur/react-ui";

type ThemeColor = string;

export const normalTheme = {
    primaryBackground: "#fff",
    backgroundColor1: "rgba(0,0,0,0.05)",
    primaryTextColor: "#3D3D3D",
    activeLinkColor: "#000",
    failedTextColor: "rgb(169, 15, 26)",
    successTextColor: "#538A1B",
    smallTextSize: "14px",
    mutedTextColor: "rgba(0, 0, 0, 0.48)",
    accentBgColor: LIGHT_THEME.blueLight,
    accentTextColor: LIGHT_THEME.btnSuccessTextColor,

    layout: {
        centered: {
            width: "1000px",
        },
    },
};

export type ITheme = typeof normalTheme;

export const darkTheme: ITheme = {
    ...normalTheme,
    primaryBackground: DARK_THEME.bgDefault,
    backgroundColor1: "rgba(0,0,0,0.25)",
    primaryTextColor: DARK_THEME.textColorDefault,
    activeLinkColor: DARK_THEME.linkActiveColor,
    failedTextColor: DARK_THEME.linkDangerColor,
    successTextColor: DARK_THEME.linkSuccessColor,
    smallTextSize: "14px",
    // 00BFFF
    mutedTextColor: DARK_THEME.textColorDisabled,
    accentBgColor: DARK_THEME.blueLight,
    accentTextColor: DARK_THEME.btnSuccessTextColor,
};

type ThemePropsPeeker<T> = <P>(props: P) => T;

type ThemeHelper<T> = T extends object ? { [K in keyof T]: ThemeHelper<T[K]> } : ThemePropsPeeker<T>;

type ThemePrimitive = string | number;

function buildThemeHelper<T extends object | ThemePrimitive>(theme: T, fn: ThemePropsPeeker<T>): ThemeHelper<T> {
    if (typeof theme === "string" || typeof theme === "number") {
        return fn as ThemeHelper<T>;
    }
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const result: any = {};
    for (const key in theme) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
        result[key] = buildThemeHelper(theme[key], props => fn(props)[key]);
    }
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
    // @ts-ignore
    // eslint-disable-next-line @typescript-eslint/no-unsafe-return
    return result;
}

// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
// eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-return
export const theme: ThemeHelper<ITheme> = buildThemeHelper(normalTheme, props => props.theme);
