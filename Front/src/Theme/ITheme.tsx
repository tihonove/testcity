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
};
