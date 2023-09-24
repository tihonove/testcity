type ThemeColor = string;

export interface ITheme {
    primaryBackground: ThemeColor;
    primaryTextColor: ThemeColor;
}

export const normalTheme: ITheme = {
    primaryBackground: "#fff",
    primaryTextColor: "#333",
}