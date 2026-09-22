/*
 * UBIS Development Deployment Pipeline
 *
 * Flow:
 * Gitea (development)
 *       |
 *       v
 * Jenkins Checkout
 *       |
 *       v
 * Detect Changed Services
 *       |
 *       v
 * Restore
 *       |
 *       v
 * Build + Publish
 *       |
 *       v
 * Backup IIS
 *       |
 *       v
 * Deploy to IIS
 *       |
 *       v
 * IIS Health Check
 *
 * IMPORTANT:
 * - appsettings.json is NEVER overwritten
 * - appsettings.*.json is NEVER overwritten
 * - Old backups are NEVER deleted
 * - IIS is touched only after successful Build + Publish
 */

def getServiceMap() {
    return [

        AIM: [
            project: 'Core\\AIM\\WebApi\\AIM.WebApi.csproj',
            path:    'Core/AIM/',
            // The AIM IIS site (destination below) is bound to the 'ubis-aim' app
            // pool, not 'aim-pool' (a stale, empty, stopped pool). Stopping
            // 'aim-pool' left the live 'ubis-aim' worker holding AIM.Application.dll,
            // so the deploy robocopy /MIR retried the locked file indefinitely
            // (build #71 hung here until the worker was killed by hand). Every stage
            // reads service.appPool from this map, so this one value drives the
            // build, the stop/start and the health check.
            appPool: 'ubis-aim',
            destination: 'C:\\inetpub\\wwwroot\\ubis-aim'
        ],

        Reporting: [
            project: 'Core\\Reporting\\WebApi\\Reporting.WebApi.csproj',
            path:    'Core/Reporting/',
            appPool: 'reporting',
            destination: 'C:\\inetpub\\wwwroot\\reporting'
        ],

        ECL: [
            project: 'Domain\\ECL\\WebApi\\ECL.WebApi.csproj',
            path:    'Domain/ECL/',
            appPool: 'ubis-ecl',
            destination: 'C:\\inetpub\\wwwroot\\ubis-ecl'
        ],

        Gateway: [
            project: 'Gateway\\UBIS.ApiGateway\\UBIS.ApiGateway.csproj',
            path:    'Gateway/UBIS.ApiGateway/',
            appPool: 'ubis-gateway',
            destination: 'C:\\inetpub\\wwwroot\\ubis-gateway'
        ],

        Email: [
            project: 'Core\\Email\\WebApi\\Email.WebApi.csproj',
            path:    'Core/Email/',
            appPool: 'ubis-email',
            destination: 'C:\\inetpub\\wwwroot\\ubis-email'
        ],

        LogWriter: [
            project: 'Core\\LogWriter\\LogWriter.csproj',
            path:    'Core/LogWriter/',
            appPool: 'ubis-logwriter',
            destination: 'C:\\inetpub\\wwwroot\\ubis-logwriter'
        ],

        MenuGenerator: [
            project: 'Core\\MenuGenerator\\WebApi\\MenuGenerator.WebApi.csproj',
            path:    'Core/MenuGenerator/',
            appPool: 'ubis-menuservice',
            destination: 'C:\\inetpub\\wwwroot\\ubis-menuservice'
        ],

        PreBudget: [
            project: 'Domain\\PreBudget\\WebApi\\PreBudget.WebApi.csproj',
            path:    'Domain/PreBudget/',
            appPool: 'ubis-prebudget',
            destination: 'C:\\inetpub\\wwwroot\\ubis-prebudget'
        ],

        Reference: [
            project: 'Shared\\ReferenceData\\WebApi\\ReferenceData.WebApi.csproj',
            path:    'Shared/ReferenceData/',
            appPool: 'ubis-reference',
            destination: 'C:\\inetpub\\wwwroot\\ubis-reference'
        ],

        UserProfile: [
            project: 'Shared\\UserProfile\\WebApi\\UserProfile.WebApi.csproj',
            path:    'Shared/UserProfile/',
            appPool: 'ubis-userprofile',
            destination: 'C:\\inetpub\\wwwroot\\ubis-userprofile'
        ],

        UBIS_Web: [
            project: 'UBIS_Web\\UBIS_Web.csproj',
            path:    'UBIS_Web/',
            appPool: 'ubis-web',
            destination: 'C:\\inetpub\\wwwroot\\ubis-web'
        ]
    ]
}


pipeline {

    agent any

    options {
        skipDefaultCheckout(false)
        timestamps()
        disableConcurrentBuilds()
    }

    environment {

        DOTNET_CONFIGURATION = 'Release'

        PUBLISH_ROOT = 'C:\\PublishTemp'
        BACKUP_ROOT  = 'C:\\IIS_Backup'

        /*
         * These are only informational.
         * Actual Gitea credentials must be configured
         * in Jenkins job SCM configuration.
         */
        GIT_BRANCH_NAME = 'development'
        GITEA_REPO = 'http://10.19.116.66:7070/UBIS/UBIS_DEV.git'
    }

    stages {

        /*
         * Jenkins Pipeline SCM performs the checkout.
         *
         * DO NOT add another git checkout here.
         */
        stage('Verify Checkout') {

            steps {

                bat '''
                    @echo off

                    echo ==============================================
                    echo VERIFYING GIT CHECKOUT
                    echo ==============================================

                    echo.
                    echo Current Directory:
                    cd

                    echo.
                    echo Git Version:
                    git --version

                    echo.
                    echo Current Commit:
                    git rev-parse HEAD

                    echo.
                    echo Current Branch:
                    git branch --show-current

                    echo.
                    echo Remote:
                    git remote -v

                    echo.
                    echo ==============================================
                '''
            }
        }


        /*
         * Detect which services changed between:
         *
         * Previous successful commit
         *             |
         *             v
         * Current commit
         *
         * If previous successful commit is not available,
         * first deployment mode is enabled.
         */
        stage('Detect Changed Services') {

            steps {

                script {

                    def serviceMap = getServiceMap()

                    def currentCommit = bat(
                        script: '@git rev-parse HEAD',
                        returnStdout: true
                    ).trim()

                    echo "Current Commit: ${currentCommit}"

                    def previousCommit = env.GIT_PREVIOUS_SUCCESSFUL_COMMIT?.trim()

                    def changedServices = []

                    /*
                     * FIRST DEPLOYMENT
                     */
                    if (!previousCommit) {

                        echo ''
                        echo '=============================================='
                        echo 'FIRST DEPLOYMENT'
                        echo 'No previous successful commit found.'
                        echo 'All services will be deployed.'
                        echo '=============================================='
                        echo ''

                        changedServices = serviceMap.keySet() as List
                    }

                    /*
                     * NORMAL DEPLOYMENT
                     */
                    else {

                        echo ''
                        echo 'Previous Successful Commit: ' + previousCommit
                        echo 'Current Commit: ' + currentCommit
                        echo ''

                        def changedFilesOutput = bat(
                            script: """
                                @echo off
                                git diff --name-only "${previousCommit}" "${currentCommit}"
                            """,
                            returnStdout: true
                        ).trim()

                        if (!changedFilesOutput) {

                            echo 'No files changed.'

                            changedServices = []

                        } else {

                            def changedFiles = changedFilesOutput
                                .split(/\r?\n/)
                                .collect { it.trim() }
                                .findAll { it }

                            echo ''
                            echo 'Changed Files:'
                            changedFiles.each {
                                echo " - ${it}"
                            }
                            echo ''

                            /*
                             * Common files affect ALL services.
                             */
                            def deployAll = false

                            changedFiles.each { file ->

                                def normalizedFile = file
                                    .replace('\\', '/')
                                    .trim()

                                if (
                                    normalizedFile == 'Jenkinsfile' ||
                                    normalizedFile.endsWith('.sln') ||
                                    normalizedFile == 'Directory.Build.props' ||
                                    normalizedFile == 'Directory.Build.targets'
                                ) {

                                    deployAll = true
                                }
                            }

                            if (deployAll) {

                                echo ''
                                echo 'Common build/deployment file changed.'
                                echo 'All services will be deployed.'
                                echo ''

                                changedServices = serviceMap.keySet() as List

                            } else {

                                /*
                                 * Detect service based on folder path.
                                 */
                                serviceMap.each { serviceName, service ->

                                    def servicePath = service.path
                                        .replace('\\', '/')
                                        .toLowerCase()

                                    def serviceChanged = changedFiles.any { file ->

                                        def normalizedFile = file
                                            .replace('\\', '/')
                                            .toLowerCase()
                                            .trim()

                                        return normalizedFile.startsWith(servicePath)
                                    }

                                    if (serviceChanged) {

                                        changedServices.add(serviceName)
                                    }
                                }
                            }
                        }
                    }

                    /*
                     * Remove duplicates while keeping order.
                     */
                    changedServices = changedServices.unique()

                    if (changedServices) {

                        env.CHANGED_SERVICES = changedServices.join(',')

                        echo ''
                        echo '=============================================='
                        echo 'SERVICES TO BUILD / DEPLOY'
                        echo '=============================================='

                        changedServices.each {
                            echo " - ${it}"
                        }

                        echo '=============================================='
                        echo ''

                    } else {

                        env.CHANGED_SERVICES = ''

                        echo ''
                        echo '=============================================='
                        echo 'NO SERVICES REQUIRE DEPLOYMENT'
                        echo '=============================================='
                        echo ''
                    }
                }
            }
        }


        /*
         * Restore ONLY the selected service projects.
         *
         * No root-level:
         *     dotnet restore
         *
         * because repository root does not contain a solution/project.
         */
        stage('Restore') {

            when {
                expression {
                    return env.CHANGED_SERVICES?.trim()
                }
            }

            steps {

                script {

                    def serviceMap = getServiceMap()

                    def servicesToBuild = env.CHANGED_SERVICES
                        .split(',')
                        .collect { it.trim() }
                        .findAll { it }

                    servicesToBuild.each { serviceName ->

                        def service = serviceMap[serviceName]

                        echo ''
                        echo '=============================================='
                        echo "Restoring: ${serviceName}"
                        echo "Project:   ${service.project}"
                        echo '=============================================='
                        echo ''

                        if (!fileExists(service.project)) {

                            error(
                                "Project file not found for ${serviceName}: ${service.project}"
                            )
                        }

                        bat """
                            @echo off

                            dotnet restore "${service.project}"

                            if errorlevel 1 (
                                echo.
                                echo RESTORE FAILED: ${serviceName}
                                exit /B 1
                            )

                            echo.
                            echo RESTORE SUCCESS: ${serviceName}
                            echo.
                        """
                    }
                }
            }
        }


        /*
         * Build + Publish all changed services.
         *
         * IIS is NOT touched in this stage.
         *
         * Therefore:
         * Build failure = live IIS remains untouched.
         */
        stage('Build and Publish') {

            when {
                expression {
                    return env.CHANGED_SERVICES?.trim()
                }
            }

            steps {

                script {

                    def serviceMap = getServiceMap()

                    def servicesToBuild = env.CHANGED_SERVICES
                        .split(',')
                        .collect { it.trim() }
                        .findAll { it }

                    servicesToBuild.each { serviceName ->

                        def service = serviceMap[serviceName]

                        def publishPath =
                            "${env.PUBLISH_ROOT}\\${serviceName}"

                        echo ''
                        echo '=============================================='
                        echo "BUILDING: ${serviceName}"
                        echo "Project:  ${service.project}"
                        echo "Publish:  ${publishPath}"
                        echo '=============================================='
                        echo ''

                        bat """
                            @echo off

                            if not exist "${service.project}" (
                                echo.
                                echo ERROR: Project file does not exist.
                                echo ${service.project}
                                exit /B 1
                            )

                            if exist "${publishPath}" (
                                echo Cleaning old publish directory...
                                rmdir /S /Q "${publishPath}"
                            )

                            mkdir "${publishPath}"

                            echo.
                            echo ----------------------------------------------
                            echo DOTNET BUILD
                            echo ----------------------------------------------
                            echo.

                            dotnet build "${service.project}" ^
                                --configuration "${env.DOTNET_CONFIGURATION}" ^
                                --no-restore

                            if errorlevel 1 (
                                echo.
                                echo BUILD FAILED: ${serviceName}
                                echo IIS deployment will NOT happen.
                                echo.
                                exit /B 1
                            )

                            echo.
                            echo BUILD SUCCESS: ${serviceName}
                            echo.

                            echo.
                            echo ----------------------------------------------
                            echo DOTNET PUBLISH
                            echo ----------------------------------------------
                            echo.

                            dotnet publish "${service.project}" ^
                                --configuration "${env.DOTNET_CONFIGURATION}" ^
                                --output "${publishPath}" ^
                                --no-restore

                            if errorlevel 1 (
                                echo.
                                echo PUBLISH FAILED: ${serviceName}
                                echo IIS deployment will NOT happen.
                                echo.
                                exit /B 1
                            )

                            echo.
                            echo PUBLISH SUCCESS: ${serviceName}
                            echo.

                            if not exist "${publishPath}" (
                                echo.
                                echo ERROR: Publish directory was not created.
                                exit /B 1
                            )
                        """
                    }
                }
            }
        }


        /*
         * Backup current IIS files and deploy.
         *
         * IMPORTANT:
         *
         * appsettings.json
         * appsettings.*.json
         *
         * are excluded from BOTH:
         *
         * 1. Backup
         * 2. Deployment
         *
         * Old backups are never deleted.
         *
         * NOTE:
         * Before stopping the app pool, we verify it exists.
         * If it does not exist (first deployment on a new server,
         * or the pool was never created / was deleted), it is
         * created automatically with "No Managed Code" runtime
         * (correct for .NET Core / .NET 5+ apps) instead of
         * failing the whole deployment.
         */
        stage('Backup and Deploy to IIS') {

            when {
                expression {
                    return env.CHANGED_SERVICES?.trim()
                }
            }

            steps {

                script {

                    def serviceMap = getServiceMap()

                    def servicesToDeploy = env.CHANGED_SERVICES
                        .split(',')
                        .collect { it.trim() }
                        .findAll { it }

                    servicesToDeploy.each { serviceName ->

                        def service = serviceMap[serviceName]

                        def livePath =
                            service.destination

                        def backupPath =
                            "${env.BACKUP_ROOT}\\${serviceName}\\Build-${env.BUILD_NUMBER}"

                        def publishPath =
                            "${env.PUBLISH_ROOT}\\${serviceName}"

                        echo ''
                        echo '=============================================='
                        echo "DEPLOYING: ${serviceName}"
                        echo "Live Path: ${livePath}"
                        echo "Backup:    ${backupPath}"
                        echo "Publish:   ${publishPath}"
                        echo "App Pool:  ${service.appPool}"
                        echo '=============================================='
                        echo ''

                        bat """
                            @echo off

                            echo ==============================================
                            echo PRE-DEPLOYMENT CHECK
                            echo ==============================================

                            if not exist "${publishPath}" (
                                echo.
                                echo ERROR: Publish directory does not exist:
                                echo ${publishPath}
                                echo.
                                exit /B 1
                            )

                            echo.
                            echo Publish directory verified.
                            echo.

                            echo ==============================================
                            echo CREATING IIS BACKUP
                            echo ==============================================

                            if exist "${livePath}" (
                                if not exist "${backupPath}" (
                                    mkdir "${backupPath}"
                                )

                                echo.
                                echo Source:
                                echo ${livePath}

                                echo.
                                echo Backup:
                                echo ${backupPath}

                                echo.

                                robocopy "${livePath}" "${backupPath}" /E /XF "appsettings.json" "appsettings.*.json" /R:3 /W:10

                                if errorlevel 8 (
                                    echo.
                                    echo ==============================================
                                    echo BACKUP FAILED
                                    echo ==============================================
                                    echo Service: ${serviceName}
                                    echo.
                                    echo IIS deployment CANCELLED.
                                    echo.
                                    exit /B 1
                                )

                                echo.
                                echo ==============================================
                                echo BACKUP SUCCESS
                                echo ==============================================
                                echo Service: ${serviceName}
                                echo Backup: ${backupPath}
                                echo ==============================================
                                echo.

                            ) else (
                                echo.
                                echo IIS directory does not exist:
                                echo ${livePath}
                                echo.
                                echo No existing files to backup.
                                echo This will be treated as first deployment.
                                echo.
                            )

                            echo ==============================================
                            echo VERIFYING IIS APP POOL EXISTS
                            echo ==============================================

                            "%windir%\\system32\\inetsrv\\appcmd.exe" list apppool /name:"${service.appPool}" >nul 2>&1

                            if errorlevel 1 (
                                echo.
                                echo App Pool "${service.appPool}" does not exist.
                                echo Creating it now with "No Managed Code" runtime...
                                echo.

                                "%windir%\\system32\\inetsrv\\appcmd.exe" add apppool /name:"${service.appPool}" /managedRuntimeVersion:""

                                if errorlevel 1 (
                                    echo.
                                    echo ERROR: Failed to create IIS App Pool.
                                    echo App Pool: ${service.appPool}
                                    echo.
                                    exit /B 1
                                )

                                echo.
                                echo App Pool "${service.appPool}" created successfully.
                                echo NOTE: If this is a brand new pool, make sure the
                                echo IIS site/application is bound to it manually
                                echo ^(this pipeline does not create sites/bindings^).
                                echo.

                            ) else (
                                echo.
                                echo App Pool "${service.appPool}" already exists.
                                echo.
                            )

                            echo ==============================================
                            echo STOPPING IIS APP POOL
                            echo ==============================================

                            set "POOL_STATE="
                            "%windir%\\system32\\inetsrv\\appcmd.exe" list apppool /name:"${service.appPool}" /text:state > "%TEMP%\\poolstate_${serviceName}.txt" 2>nul
                            set /p POOL_STATE=<"%TEMP%\\poolstate_${serviceName}.txt"
                            del /Q "%TEMP%\\poolstate_${serviceName}.txt" 2>nul

                            echo Current state: %POOL_STATE%

                            if /I "%POOL_STATE%"=="Stopped" (
                                echo.
                                echo App Pool "${service.appPool}" is already stopped.
                                echo Skipping stop command.
                                echo.
                            ) else (
                                "%windir%\\system32\\inetsrv\\appcmd.exe" stop apppool /apppool.name:"${service.appPool}"

                                if errorlevel 1 (
                                    echo.
                                    echo ERROR: Failed to stop IIS App Pool.
                                    echo App Pool: ${service.appPool}
                                    echo.
                                    exit /B 1
                                )

                                echo.
                                echo IIS App Pool stop requested.
                                echo.
                            )

                            REM ------------------------------------------------------------------
                            REM 'appcmd stop apppool' returns before the worker process actually
                            REM exits and releases its loaded DLLs. Poll until the pool reports
                            REM Stopped (~30s max); if it still has not, terminate the pool's
                            REM worker process(es) so the /MIR robocopy below cannot retry a
                            REM locked file forever (build #71 hung here for ~20 min - robocopy's
                            REM default is /R:1000000 /W:30). Belt-and-braces on top of the
                            REM getServiceMap() AIM app-pool-name fix.
                            REM ------------------------------------------------------------------
                            echo Waiting for app pool "${service.appPool}" to reach Stopped...
                            set /a POOL_WAIT=0

                            :POOLWAIT_${serviceName}
                            set "POOL_NOW="
                            "%windir%\\system32\\inetsrv\\appcmd.exe" list apppool /name:"${service.appPool}" /text:state > "%TEMP%\\poolwait_${serviceName}.txt" 2>nul
                            set /p POOL_NOW=<"%TEMP%\\poolwait_${serviceName}.txt"
                            del /Q "%TEMP%\\poolwait_${serviceName}.txt" 2>nul
                            if /I "%POOL_NOW%"=="Stopped" goto POOLDOWN_${serviceName}
                            set /a POOL_WAIT+=1
                            if %POOL_WAIT% GEQ 15 goto POOLKILL_${serviceName}
                            ping -n 3 127.0.0.1 >nul
                            goto POOLWAIT_${serviceName}

                            :POOLKILL_${serviceName}
                            echo App pool "${service.appPool}" not Stopped after wait - terminating its worker process(es)...
                            "%windir%\\system32\\inetsrv\\appcmd.exe" list wp > "%TEMP%\\wp_${serviceName}.txt" 2>nul
                            for /f "tokens=2 delims= " %%P in ('findstr /i /c:"(applicationPool:${service.appPool})" "%TEMP%\\wp_${serviceName}.txt"') do (
                                echo   Killing worker PID %%~P
                                taskkill /f /pid %%~P >nul 2>&1
                            )
                            del /Q "%TEMP%\\wp_${serviceName}.txt" 2>nul
                            ping -n 3 127.0.0.1 >nul

                            :POOLDOWN_${serviceName}
                            echo App pool "${service.appPool}" is stopped.
                            echo.

                            echo ==============================================
                            echo DEPLOYING FILES
                            echo ==============================================

                            echo.
                            echo Source:
                            echo ${publishPath}

                            echo.
                            echo Destination:
                            echo ${livePath}

                            echo.

                            robocopy "${publishPath}" "${livePath}" /MIR /XF "appsettings.json" "appsettings.*.json" /R:3 /W:10

                            if errorlevel 8 (
                                echo.
                                echo ==============================================
                                echo DEPLOYMENT FAILED
                                echo ==============================================
                                echo Service: ${serviceName}
                                echo.
                                echo Attempting to restart IIS App Pool...
                                echo.

                                "%windir%\\system32\\inetsrv\\appcmd.exe" start apppool /apppool.name:"${service.appPool}"

                                echo.
                                echo IIS App Pool restart attempted.
                                echo.
                                exit /B 1
                            )

                            echo.
                            echo ==============================================
                            echo FILE DEPLOYMENT SUCCESS
                            echo ==============================================
                            echo Service: ${serviceName}
                            echo ==============================================
                            echo.

                            echo ==============================================
                            echo STARTING IIS APP POOL
                            echo ==============================================

                            set "POOL_STATE2="
                            "%windir%\\system32\\inetsrv\\appcmd.exe" list apppool /name:"${service.appPool}" /text:state > "%TEMP%\\poolstate2_${serviceName}.txt" 2>nul
                            set /p POOL_STATE2=<"%TEMP%\\poolstate2_${serviceName}.txt"
                            del /Q "%TEMP%\\poolstate2_${serviceName}.txt" 2>nul

                            echo Current state: %POOL_STATE2%

                            if /I "%POOL_STATE2%"=="Started" (
                                echo.
                                echo App Pool "${service.appPool}" is already started.
                                echo Skipping start command.
                                echo.
                            ) else (
                                "%windir%\\system32\\inetsrv\\appcmd.exe" start apppool /apppool.name:"${service.appPool}"

                                if errorlevel 1 (
                                    echo.
                                    echo ERROR: Failed to start IIS App Pool.
                                    echo App Pool: ${service.appPool}
                                    echo.
                                    exit /B 1
                                )

                                echo.
                                echo IIS App Pool started successfully.
                                echo.
                            )

                            echo ==============================================
                            echo DEPLOYMENT COMPLETED
                            echo ==============================================
                            echo Service: ${serviceName}
                            echo ==============================================
                            echo.
                        """
                    }
                }
            }
        }


        /*
         * Verify that deployed IIS app pools are running.
         */
        stage('IIS Health Check') {

            when {
                expression {
                    return env.CHANGED_SERVICES?.trim()
                }
            }

            steps {

                script {

                    def serviceMap = getServiceMap()

                    def servicesToCheck = env.CHANGED_SERVICES
                        .split(',')
                        .collect { it.trim() }
                        .findAll { it }

                    servicesToCheck.each { serviceName ->

                        def service = serviceMap[serviceName]

                        echo ''
                        echo "Checking IIS App Pool: ${service.appPool}"

                        bat """
                            @echo off

                            "%windir%\\system32\\inetsrv\\appcmd.exe" list apppool "${service.appPool}"

                            if errorlevel 1 (
                                echo.
                                echo ERROR: Could not query IIS App Pool.
                                echo ${service.appPool}
                                exit /B 1
                            )

                            echo.
                            echo IIS App Pool check completed.
                            echo.
                        """
                    }
                }
            }
        }
    }


    post {

        success {

            echo ''
            echo '================================================'
            echo 'JENKINS BUILD SUCCESSFUL'
            echo '================================================'
            echo ''
            echo "Build Number: ${env.BUILD_NUMBER}"
            echo "Commit:       ${env.GIT_COMMIT ?: 'N/A'}"
            echo "Services:     ${env.CHANGED_SERVICES ?: 'None'}"
            echo ''
            echo 'Deployment completed successfully.'
            echo '================================================'
            echo ''
        }

        failure {

            echo ''
            echo '================================================'
            echo 'JENKINS BUILD FAILED'
            echo '================================================'
            echo ''
            echo "Build Number: ${env.BUILD_NUMBER}"
            echo "Services:     ${env.CHANGED_SERVICES ?: 'None'}"
            echo ''
            echo 'Check the Jenkins console log for the exact failure.'
            echo '================================================'
            echo ''
        }

        aborted {

            echo ''
            echo '================================================'
            echo 'JENKINS BUILD ABORTED'
            echo '================================================'
            echo ''
        }
    }
}