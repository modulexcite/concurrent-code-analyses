SET /a c=20
SET /a i=0
SET path=D:\C#PROJECTS\XAMLProjects
:loop
IF %i%==1050 GOTO END
Collector %c% %path%
SET /a i=%i%+ %c%
GOTO LOOP
:end

PAUSE