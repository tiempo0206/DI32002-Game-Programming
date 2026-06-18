#!/usr/bin/env python3

import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class UnityWebGLRequestHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(".br"):
            return super().guess_type(path[:-3])
        if path.endswith(".gz"):
            return super().guess_type(path[:-3])
        return super().guess_type(path)

    def end_headers(self):
        if self.path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif self.path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")

        self.send_header("Cache-Control", "no-cache")
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description="Serve a compressed Unity WebGL build locally.")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument(
        "--build-dir",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "Builds" / "WebGL",
    )
    arguments = parser.parse_args()
    build_dir = arguments.build_dir.resolve()

    if not (build_dir / "index.html").is_file():
        parser.error(f"No WebGL index.html found in {build_dir}")

    handler = partial(UnityWebGLRequestHandler, directory=str(build_dir))
    server = ThreadingHTTPServer(("127.0.0.1", arguments.port), handler)
    print(f"Serving {build_dir} at http://127.0.0.1:{arguments.port}")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
