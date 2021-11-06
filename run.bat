echo off
cls
echo Compiler Output:
.\MentalCaressCompiler\bin\debug\net5.0\MentalCaressCompiler.exe -out="%1.bf" %1
type %1.bf
echo.
echo.
echo Running...
.\BrainfuckRun\bin\debug\net5.0\BrainfuckRun.exe %1.bf
del %1.bf
