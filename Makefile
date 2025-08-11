.PHONY: tools-restore
tools-restore:
	cd src && dotnet tool restore

.PHONY: format
format:
	cd src && dotnet csharpier format .

.PHONY: rebuild
rebuild:
	dotnet build --no-incremental

.PHONY: run
run:
	cd src/Enerca.EnerkomInternalPrice.Main && dotnet run
