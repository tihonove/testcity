import { DARK_THEME, LIGHT_THEME } from "@skbkontur/react-ui";

type ThemeColor = string;

export interface ITheme {
    primaryBackground: ThemeColor;
    primaryTextColor: ThemeColor;

    activeLinkColor: ThemeColor;

    backgroundColor1: ThemeColor;
    failedTextColor: ThemeColor;
    successTextColor: ThemeColor;
    smallTextSize: ThemeColor;
    mutedTextColor: ThemeColor;
    accentBgColor: ThemeColor;
    accentTextColor: ThemeColor;
}

export const normalTheme: ITheme = {
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
};

const a = DARK_THEME;

export const darkTheme: ITheme = {
    primaryBackground: DARK_THEME.bgDefault,
    backgroundColor1: "rgba(0,0,0,0.05)",
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
