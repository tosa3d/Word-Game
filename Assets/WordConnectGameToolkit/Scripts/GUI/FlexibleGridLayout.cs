using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace WordsToolkit.Scripts.GUI
{
    [AddComponentMenu("Layout/Flexible Grid Layout")]
    public class FlexibleGridLayout : LayoutGroup
    {
        [Header("Grid Settings")]
        [SerializeField] private bool fitX = true;
        [SerializeField] private bool fitY = true;
        [SerializeField] private int rows = 1;
        [SerializeField] private int columns = 1;
        
        [Header("Cell Settings")]
        [SerializeField] private Vector2 cellSize = new Vector2(100, 100);
        [SerializeField] private Vector2 spacing = Vector2.zero;
        [SerializeField] private bool overrideHeight = false;
        [SerializeField] private float fixedCellHeight = 100f;
        
        [Header("Flexible Width Options")]
        [SerializeField] private bool enableFlexibleWidth = true;
        [SerializeField] private bool enableFlexibleHeight = false;
        [SerializeField] private float minCellWidth = 50f;
        [SerializeField] private float maxCellWidth = 200f;
        [SerializeField] private bool keepAspectRatio = false;
        [SerializeField] private bool stretchLastRow = true;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            if (fitX || fitY)
            {
                float sqrRt = Mathf.Sqrt(transform.childCount);
                rows = Mathf.CeilToInt(sqrRt);
                columns = Mathf.CeilToInt(sqrRt);
            }

            if (fitX)
            {
                rows = Mathf.CeilToInt(transform.childCount / (float)columns);
            }
            if (fitY)
            {
                columns = Mathf.CeilToInt(transform.childCount / (float)rows);
            }

            CalculateAndApplyLayout();
        }

        private void CalculateAndApplyLayout()
        {
            float parentWidth = rectTransform.rect.width;
            float parentHeight = rectTransform.rect.height;

            float availableWidth = parentWidth - padding.left - padding.right;
            float availableHeight = parentHeight - padding.top - padding.bottom;

            float totalSpacingWidth = spacing.x * (columns - 1);
            float totalSpacingHeight = spacing.y * (rows - 1);

            float cellWidth = (availableWidth - totalSpacingWidth) / columns;
            float cellHeight = (availableHeight - totalSpacingHeight) / rows;

            if (enableFlexibleWidth)
            {
                cellWidth = Mathf.Clamp(cellWidth, minCellWidth, maxCellWidth);
                if (keepAspectRatio)
                {
                    cellHeight = cellWidth * (cellSize.y / cellSize.x);
                }
            }

            if (enableFlexibleHeight && !keepAspectRatio)
            {
                cellHeight = enableFlexibleHeight ? cellHeight : cellSize.y;
            }

            cellSize.x = enableFlexibleWidth ? cellWidth : cellSize.x;
            cellSize.y = enableFlexibleHeight || keepAspectRatio ? cellHeight : cellSize.y;

            // Override height if specified
            if (overrideHeight)
            {
                cellSize.y = fixedCellHeight;
            }

            SetChildrenPositions();
        }

        private void SetChildrenPositions()
        {
            int totalChildren = rectChildren.Count;
            int lastRowStart = ((totalChildren - 1) / columns) * columns;
            int elementsInLastRow = totalChildren - lastRowStart;
            bool isLastRowIncomplete = elementsInLastRow < columns && elementsInLastRow > 0;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                int rowIndex = i / columns;
                int columnIndex = i % columns;
                bool isInLastRow = i >= lastRowStart;

                var item = rectChildren[i];

                float cellWidth = cellSize.x;
                float xPos;

                if (stretchLastRow && isLastRowIncomplete && isInLastRow)
                {
                    // Calculate stretched width for last row elements
                    float availableWidth = rectTransform.rect.width - padding.left - padding.right;
                    float totalSpacing = spacing.x * (elementsInLastRow - 1);
                    float stretchedCellWidth = (availableWidth - totalSpacing) / elementsInLastRow;
                    
                    if (enableFlexibleWidth)
                    {
                        stretchedCellWidth = Mathf.Clamp(stretchedCellWidth, minCellWidth, maxCellWidth);
                    }
                    
                    cellWidth = stretchedCellWidth;
                    
                    // Calculate position for stretched elements
                    int positionInLastRow = i - lastRowStart;
                    xPos = padding.left + (cellWidth * positionInLastRow) + (spacing.x * positionInLastRow);
                }
                else
                {
                    // Normal positioning
                    xPos = padding.left + (cellSize.x * columnIndex) + (spacing.x * columnIndex);
                }

                float yPos = padding.top + (cellSize.y * rowIndex) + (spacing.y * rowIndex);

                SetChildAlongAxis(item, 0, xPos, cellWidth);
                SetChildAlongAxis(item, 1, yPos, cellSize.y);
            }
        }

        public override void CalculateLayoutInputVertical()
        {
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }
    }
}
