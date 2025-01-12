@echo off
setlocal

REM 定義 lpMethod 和 pgradient 的範圍
set lpMethods=1 2
set pGradients=-1 0 1 2 3 4

REM 迭代 lpMethod 和 pgradient 的組合
for %%m in (%lpMethods%) do (
    for %%p in (%pGradients%) do (
        echo Running with lpMethod=%%m and pgradient=%%p
        HW5.exe %%m %%p
    )
)

endlocal
