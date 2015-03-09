
check: ConcreteClasses.dll NonConcreteReflection.exe 
	mono --debug NonConcreteReflection.exe

NonConcreteReflection.exe: NonConcreteReflection.cs SharedClasses.dll 
	mcs -t:exe NonConcreteReflection.cs /reference:SharedClasses.dll

SharedClasses.dll: SharedClasses.cs
	mcs -t:library SharedClasses.cs

ConcreteClasses.dll: ConcreteClasses.cs SharedClasses.dll
	mcs -t:library ConcreteClasses.cs /reference:SharedClasses.dll

.PHONY: clean
clean:
	rm -f *.exe *.dll *.mdb

