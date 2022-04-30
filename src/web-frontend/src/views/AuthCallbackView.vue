<template>


      <v-container fill-height fluid>
        <v-row align="center" justify="center">
        <v-progress-circular indeterminate :rotate="360" :size="180" :width="15" color="#1976d2">
          Logging in...
        </v-progress-circular>
        </v-row>
      </v-container>  


</template>

<script>
import axios from "axios";

export default {
  name: "AuthCallbackView",
  data() {
    return {
      interval: {},
    };
  },
  mounted: function() {
    // check every second until we are authenticated, and then load the dashboard
    this.interval = setInterval(() => {
        if (!this.$auth.loading && this.$auth.isAuthenticated) {
          clearInterval(this.interval);
          this.verifyAndNavigate();
        }
      }, 1000)    
    
  },
  methods: {
    async verifyAndNavigate() {

// Get the access token from the auth wrapper
      const token = await this.$auth.getTokenSilently();
      //alert(token);
      //var userId = this.$auth.user.sub;

      // Use Axios to make a call to the API
      var url = `${process.env.VUE_APP_API_SERVER_URL}/user/current/ensure-exists`;
      await axios.get(url, {
         headers: {
           Authorization: `Bearer ${token}`    // send the access token through the 'Authorization' header
         }
      });

      this.$router.replace({ path: '/dashboard' })
    }
  }
};
</script>
