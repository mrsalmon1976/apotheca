<template>
  <v-app>
    <v-app-bar
      app
      color="primary"
      dark
    >
      <div class="d-flex align-center">
        <router-link to="/">
          <v-img alt="Vuetify Logo" class="shrink mr-2" contain src="https://cdn.vuetifyjs.com/images/logos/vuetify-logo-dark.png" transition="scale-transition"
            width="40"
          />
        </router-link>

        <router-link to="/">
          <v-img
            alt="Vuetify Name"
            class="shrink mt-1 hidden-sm-and-down"
            contain
            min-width="100"
            src="https://cdn.vuetifyjs.com/images/logos/vuetify-name-dark.png"
            width="100"
            :title="versionTitle"
          />
        </router-link>
      </div>

      <v-spacer></v-spacer>


      <v-btn :disabled="$auth.loading || !$auth.isAuthenticated" href="/profile" text>
        <span class="mr-2">Profile</span>
      </v-btn>
      <v-btn v-if="!$auth.loading && !$auth.isAuthenticated" @click="login" text>
        <span class="mr-2">Log In</span>
      </v-btn>
      <v-btn v-if="!$auth.loading && $auth.isAuthenticated" @click="logout" text>
        <span class="mr-2">Log Out</span>
      </v-btn>

      <v-btn
        href="https://github.com/vuetifyjs/vuetify/releases/latest"
        target="_blank"
        text
      >
        <span class="mr-2">Latest Release</span>
        <v-icon>mdi-open-in-new</v-icon>
      </v-btn>
    </v-app-bar>

    <v-main>
      <router-view/>
    </v-main>
  </v-app>
</template>

<script>

import { versionService } from "@/services/VersionService";

export default {
  name: 'App',

  data: function() {
    return {
      versionInfo: null,
      versionTitle: "",
    }
  },
  created: function() {
    versionService.getVersion().then(vi => { 
      this.versionInfo = vi
      this.versionTitle = "Apotheca version " + vi.version
    });
  },
  methods: {
    // Log the user in
    login() {
      this.$auth.loginWithRedirect();
    },
    // Log the user out
    logout() {
      this.$auth.logout({
        returnTo: window.location.origin
      });
    },
  }
};
</script>
