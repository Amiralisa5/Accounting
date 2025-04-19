pipeline {
    agent any

    stages {
        stage('Build') {
            steps {
                echo 'Building..'
                echo 'Building dev..'
				sh 'pwsh ./build.ps1 -PreventMigration'
            }
        }
        stage('Test') {
            steps {
                echo 'Testing..'
				sh 'pwsh ./test.ps1 -PreventMigration'
            }
        }
        stage('Deploy') {
            steps {
                echo 'Deploying....'				
            }
        }
    }
}