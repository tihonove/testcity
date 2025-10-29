import * as React from "react";

export interface UserInfo {
    userName: string | null;
    displayName: string | null;
    avatarUrl: string | null;
}

interface UserContextValue {
    user: UserInfo;
}

const UserContext = React.createContext<UserContextValue | null>(null);

interface UserProviderProps {
    user: UserInfo;
    children: React.ReactNode;
}

export function UserProvider({ user, children }: UserProviderProps) {
    const contextValue = React.useMemo(() => ({ user }), [user]);

    return <UserContext.Provider value={contextValue}>{children}</UserContext.Provider>;
}

export function useUser(): UserInfo {
    const context = React.useContext(UserContext);
    if (!context) {
        throw new Error("useUser должен использоваться внутри UserProvider");
    }
    return context.user;
}
