import { DARK_THEME, LIGHT_THEME } from "@skbkontur/react-ui";

type ThemeColor = string;

export const normalTheme = {
    primaryBackground: "#fff",
    backgroundColor1: "rgba(0,0,0,0.05)",
    backgroundColor2: "rgba(0,0,0,0.05)",
    inverseColor: (fraction: string) => `rgba(0,0,0,${fraction})`,
    primaryTextColor: "#3D3D3D",
    activeLinkColor: "#000",
    failedTextColor: "rgb(169, 15, 26)",
    successTextColor: "#538A1B",
    smallTextSize: "14px",
    mutedTextColor: "rgba(0, 0, 0, 0.48)",
    accentBgColor: LIGHT_THEME.blueLight,
    failedBgColor: LIGHT_THEME.btnDangerBg,
    accentTextColor: LIGHT_THEME.btnSuccessTextColor,
    borderLineColor2: "rgba(0, 0, 0, 0.1)",

    typography: {
        pages: {
            header1: "font-size: 28px; line-height: 36px;",
        },
    },

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
    backgroundColor2: "rgba(255,255,255,0.05)",
    inverseColor: (fraction: string) => `rgba(255,255,255,${fraction})`,
    primaryTextColor: DARK_THEME.textColorDefault,
    activeLinkColor: DARK_THEME.linkActiveColor,
    failedTextColor: DARK_THEME.linkDangerColor,
    successTextColor: DARK_THEME.linkSuccessColor,
    smallTextSize: "14px",
    // 00BFFF
    mutedTextColor: DARK_THEME.textColorDisabled,
    failedBgColor: DARK_THEME.btnDangerBg,
    accentBgColor: DARK_THEME.blueLight,
    accentTextColor: DARK_THEME.btnSuccessTextColor,
    borderLineColor2: "rgba(255, 255, 255, 0.1)",
};

type ThemePropsPeeker<T> = <P>(props: P) => T;
type ThemeFuncPropsPeeker<TResult, TA1> = (a1: TA1) => ThemePropsPeeker<TResult>;

/* eslint-disable prettier/prettier */
type ThemeHelper<T> = 
    T extends (a1: infer TA1) => infer TResult ? ThemeFuncPropsPeeker<TResult, TA1> :
    T extends object ? { [K in keyof T]: ThemeHelper<T[K]> } 
    : ThemePropsPeeker<T>;
/* eslint-enable prettier/prettier */

type ThemePrimitive = string | number;

function buildThemeHelper<T extends object | ThemePrimitive>(theme: T, fn: ThemePropsPeeker<T>): ThemeHelper<T> {
    if (typeof theme === "string" || typeof theme === "number") {
        return fn as ThemeHelper<T>;
    }
    if (typeof theme === "function") {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        // eslint-disable-next-line @typescript-eslint/no-unsafe-return, prettier/prettier, @typescript-eslint/no-unsafe-argument
        return ((...args) => props => fn(props)(...args)) as ThemeHelper<T>;
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
