cd ./compose
docker compose down -v
docker compose stop && docker compose rm -f -v
cd .. && sh ./run_infra.sh
cd ./src && sh ./run_all_migrations.sh