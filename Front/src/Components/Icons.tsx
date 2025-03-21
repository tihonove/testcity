import {
    ArrowARightIcon16Regular,
    ArrowARightIcon24Regular,
    ArrowARightIcon32Regular,
    BuildingHomeIcon16Regular,
    BuildingHomeIcon24Regular,
    BuildingHomeIcon32Regular,
    ShapeCircleMIcon16Regular,
    ShapeCircleMIcon24Regular,
    ShapeCircleMIcon32Regular,
    ShapeSquareIcon16Regular,
    ShapeSquareIcon24Regular,
    ShapeSquareIcon32Regular,
} from "@skbkontur/icons";
import * as React from "react";
import { RunStatus } from "./RunStatus";

export function JonIcon(props: { size: 16 | 24 | 32; status?: RunStatus }) {
    switch (props.size) {
        case 16:
            return <ShapeSquareIcon16Regular />;
        case 24:
            return <ShapeSquareIcon24Regular />;
        case 32:
            return <ShapeSquareIcon32Regular />;
    }
}

export function JonRunIcon(props: { size: 16 | 24 | 32; status?: RunStatus }) {
    switch (props.size) {
        case 16:
            return <ShapeCircleMIcon16Regular />;
        case 24:
            return <ShapeCircleMIcon24Regular />;
        case 32:
            return <ShapeCircleMIcon32Regular />;
    }
}

export function HomeIcon(props: { size: 16 | 24 | 32 }) {
    switch (props.size) {
        case 16:
            return <BuildingHomeIcon16Regular />;
        case 24:
            return <BuildingHomeIcon24Regular />;
        case 32:
            return <BuildingHomeIcon32Regular />;
    }
}

export function ArrowARightIcon(props: { size: 16 | 24 | 32 }) {
    switch (props.size) {
        case 16:
            return <ArrowARightIcon16Regular style={{ margin: "auto 5px" }} />;
        case 24:
            return <ArrowARightIcon24Regular />;
        case 32:
            return <ArrowARightIcon32Regular />;
    }
}
