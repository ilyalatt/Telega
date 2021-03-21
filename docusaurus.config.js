/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  title: 'Telega',
  tagline: 'A simple yet powerful Telegram library',
  url: 'https://ilyalatt.github.io',
  baseUrl: '/Telega/',
  organizationName: 'ilyalatt',
  projectName: 'Telega',
  onBrokenLinks: 'throw',
  favicon: 'img/favicon.ico',
  themeConfig: {
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: true,
    },
    prism: {
      theme: require('prism-react-renderer/themes/vsDark'),
      additionalLanguages: ['csharp'],
    },
    navbar: {
      title: 'Telega',
      logo: {
        alt: 'Telega',
        src: 'img/logo.svg',
      },
      items: [
        {
          to: 'docs/',
          activeBasePath: 'docs',
          label: 'Docs',
          position: 'left',
        },
        {
          href: 'https://github.com/ilyalatt/Telega',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl: 'https://github.com/ilyalatt/Telega/edit/master/website/',
        },
        blog: {
          showReadingTime: true,
          editUrl: 'https://github.com/ilyalatt/Telega/edit/master/website/blog/',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
};
