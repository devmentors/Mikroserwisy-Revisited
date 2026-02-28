/** @type {import('next').NextConfig} */
const nextConfig = {
  basePath: '',
  async redirects() {
    return [
      {
        source: '/',
        destination: '/applications-list',
        permanent: true,
      },
    ]
  },
}

export default nextConfig