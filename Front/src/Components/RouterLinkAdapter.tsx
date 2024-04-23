import * as React from "react";
import { Link } from "react-router-dom";

export const RouterLinkAdapter = ({
    href,
    children,
    className,
    ...rest
}: {
    href: string;
    children: React.ReactNode;
    className?: string;
}) => (
    <Link className={className} to={href} {...rest}>
        {children}
    </Link>
);
