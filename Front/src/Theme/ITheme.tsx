type ThemeColor = string;

export interface ITheme {
    primaryBackground: ThemeColor;
    primaryTextColor: ThemeColor;
}

export const darkTheme: ITheme = {
    primaryBackground: "rgb(31, 31, 31)",
    primaryTextColor: "rgba(255, 255, 255, 0.867)",
}