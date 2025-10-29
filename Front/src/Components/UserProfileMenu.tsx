import { Hint, Button } from "@skbkontur/react-ui";
import * as React from "react";
import { useUser } from "../Contexts/UserContext";
import { UserAvatar } from "./UserAvatar";

export function UserProfileMenu(): React.JSX.Element {
    const user = useUser();

    return (
        <Hint text={user.userName} pos="left middle" maxWidth={400}>
            <Button use="link">
                <UserAvatar avatarUrl={user.avatarUrl} displayName={user.displayName} userName={user.userName} />
            </Button>
        </Hint>
    );
}
