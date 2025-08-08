.PHONY: rebuild
rebuild:
	dotnet clean
	dotnet build --no-incremental

.PHONY: run
run:
	cd src/Enerca.EnerkomInternalPrice.Console && dotnet run
