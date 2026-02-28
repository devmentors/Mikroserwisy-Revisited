#!/bin/bash

# Run all migrations
echo "Running all migrations..."
for dir in Services/*/*.Api/; do
    if [ -d "$dir" ]; then
        parent_dir=$(dirname "$dir")
        migration_script="$parent_dir/run-migrations.sh"
        if [ -f "$migration_script" ]; then
            echo "Found migration script in $parent_dir"
            echo "Running migrations using $migration_script"
            (cd "$parent_dir" && bash run-migrations.sh) || { echo "Migration failed in $parent_dir"; exit 1; }
        else
            echo "No migration script found in $parent_dir"
        fi
    fi
done