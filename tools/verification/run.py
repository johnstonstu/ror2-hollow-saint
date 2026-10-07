"""Run source checks concurrently, then optional runtime/native gates in order."""
import argparse
import concurrent.futures
import datetime
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]


def run_check(name, command, output):
    start = time.monotonic()
    log = output / (name + '.log')
    try:
        with log.open('w', encoding='utf-8') as stream:
            process = subprocess.run(command, cwd=ROOT, stdout=stream,
                                     stderr=subprocess.STDOUT, timeout=600)
        code = process.returncode
        status = 'passed' if code == 0 else 'unavailable' if code == 2 else 'failed'
    except (OSError, subprocess.TimeoutExpired) as error:
        with log.open('a', encoding='utf-8') as stream:
            stream.write('\n' + str(error))
        code, status = None, 'failed'
    result = dict(name=name, status=status, exit_code=code,
                  seconds=round(time.monotonic() - start, 2), log=str(log))
    print(f'{status.upper():11} {name} ({result["seconds"]}s)', flush=True)
    return result


def commands(shell):
    manifest = json.loads((ROOT / 'tools/checks.json').read_text(encoding='utf-8'))
    projects = manifest['projects']
    discovered = {p.relative_to(ROOT).as_posix() for p in (ROOT / 'tools').glob('*Checks/*.csproj')}
    if set(projects) != discovered or len(projects) != len(set(projects)):
        raise ValueError('tools/checks.json differs from *Checks projects; register every new suite explicitly')
    checks = [(Path(p).stem, ['dotnet', 'run', '--project', p, '-c', 'Release', '--', str(ROOT)])
              for p in projects]
    checks += [(Path(p).stem, [shell, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', p])
               for p in manifest['scripts']]
    checks += [('ToolingChecks', [sys.executable, '-m', 'unittest', 'discover',
                                  '-s', 'tools/verification', '-p', 'test_*.py']),
               ('VersionParity', [sys.executable, 'tools/bump.py', '--check'])]
    return checks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scope', choices=['Quick', 'Build', 'Native'], default='Quick')
    parser.add_argument('--jobs', type=int, choices=range(1, 9), default=3)
    parser.add_argument('--managed')
    parser.add_argument('--profile-bepinex')
    args = parser.parse_args()
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S-%fZ')
    output = ROOT / 'artifacts/verification' / stamp
    output.mkdir(parents=True)
    results = []
    try:
        shell = shutil.which('pwsh') or shutil.which('powershell')
        if not shell:
            raise ValueError('PowerShell is required for legacy source checks')
        work = commands(shell)
        with concurrent.futures.ThreadPoolExecutor(max_workers=args.jobs) as pool:
            futures = [pool.submit(run_check, name, command, output) for name, command in work]
            results = [future.result() for future in futures]
        if all(r['status'] == 'passed' for r in results) and args.scope != 'Quick':
            project = 'HollowSaintMod/HollowSaint.csproj'
            results.append(run_check('LockedRestore', ['dotnet', 'restore', project, '--locked-mode'], output))
            if results[-1]['status'] == 'passed':
                results.append(run_check('RuntimeBuild', ['dotnet', 'build', project, '-c', 'Release',
                    '--no-restore', '-o', str(output / 'runtime'), '-nologo', '-v', 'minimal'], output))
        if all(r['status'] == 'passed' for r in results) and args.scope == 'Native':
            results.append(run_check('AccessScannerFixtures', [shell, '-NoProfile', '-ExecutionPolicy', 'Bypass',
                '-File', 'tools/verification/Test-AccessCheck.ps1'], output))
        if all(r['status'] == 'passed' for r in results) and args.scope == 'Native':
            command = [shell, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
                       'tools/dev-profile/Check-Access.ps1', '-Dll', str(output / 'runtime/HollowSaint.dll')]
            for flag, value in [('-Managed', args.managed), ('-ProfileBepInEx', args.profile_bepinex)]:
                if value:
                    command.extend([flag, value])
            results.append(run_check('NativeMemberAccess', command, output))
    except Exception as error:
        print(f'Verification failed: {error}', file=sys.stderr)
        results.append(dict(name='Runner', status='failed', error=str(error)))
    report = dict(scope=args.scope, results=results,
                  passed=bool(results) and all(r['status'] == 'passed' for r in results),
                  limits='Native scope scans installed DLL references; no Unity, gameplay, or multiplayer execution.')
    (output / 'results.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('Report: ' + str(output / 'results.json'))
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    sys.exit(main())
