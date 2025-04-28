import React, { useState, useCallback, ReactElement } from "react";
import { ArrowCRightIcon16Regular } from "@skbkontur/icons/ArrowCRightIcon16Regular";
import { ArrowCDownIcon16Regular } from "@skbkontur/icons/ArrowCDownIcon16Regular";
import { FolderIcon16Regular } from "@skbkontur/icons/FolderIcon16Regular";
import { FolderMinusIcon16Regular } from "@skbkontur/icons/FolderMinusIcon16Regular";
import { FileTypeMarkupIcon16Regular } from "@skbkontur/icons/FileTypeMarkupIcon16Regular";
import styles from "./TreeView.module.css";

export interface TreeNode<TDetails> {
    id: string;
    name: string;
    type: "file" | "directory";
    children?: TreeNode<TDetails>[];
    path: string;
    details?: TDetails;
}

export interface TreeViewProps<TDetails> {
    data: TreeNode<TDetails>[];
    onSelect?: (node: TreeNode<TDetails>) => void;
    renderDetails?: (details: TDetails) => ReactElement;
    selectedPath?: string;
}

export function TreeView<T>({ data, onSelect, selectedPath, renderDetails }: TreeViewProps<T>) {
    const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());

    const toggleNode = useCallback((path: string) => {
        setExpandedNodes(prev => {
            const next = new Set(prev);
            if (next.has(path)) {
                next.delete(path);
            } else {
                next.add(path);
            }
            return next;
        });
    }, []);

    const renderNode = (node: TreeNode<T>, level: number = 0) => {
        const isExpanded = expandedNodes.has(node.path);
        const isSelected = node.path === selectedPath;

        return (
            <div key={node.path}>
                <div
                    className={`${styles.nodeContent} ${isSelected ? styles.nodeContentSelected : ""}`}
                    onClick={() => {
                        if (node.type === "directory") {
                            toggleNode(node.path);
                        }
                        onSelect?.(node);
                    }}
                    style={{ paddingLeft: level * 16 }}>
                    <div className={styles.iconWrapper}>
                        {node.type === "directory" &&
                            (isExpanded ? <ArrowCDownIcon16Regular /> : <ArrowCRightIcon16Regular />)}
                    </div>
                    <div className={styles.fileIconWrapper}>
                        {node.type === "directory" ? (
                            isExpanded ? (
                                <FolderMinusIcon16Regular />
                            ) : (
                                <FolderIcon16Regular />
                            )
                        ) : (
                            <FileTypeMarkupIcon16Regular />
                        )}
                    </div>
                    <span className={styles.nodeName} title={node.name}>
                        {node.name}
                    </span>
                    {node.details && renderDetails && renderDetails(node.details)}
                </div>
                {node.type === "directory" && isExpanded && node.children && (
                    <div role="group">{node.children.map(child => renderNode(child, level + 1))}</div>
                )}
            </div>
        );
    };

    return (
        <div className={styles.treeContainer} role="tree">
            {data.map(node => renderNode(node))}
        </div>
    );
}
